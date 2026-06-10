using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Services.FriendshipsService;
using MatchikoMap.Utils;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Net;

namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendshipsController(AppDbContext _db, IHubContext<ChatHub> _hub, IFriendshipsService _fs) : ControllerBase
    {
        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("list/{userId}")]
        public async Task<IActionResult> GetFriendsList(int userId)
        {
            // 1. wybranie konwersacji prywatnych z conversationMembers, zwraca ConversationId, FriendId, i moje LastReadAt
            var conversations = await (
                from cm1 in _db.ConversationMembers
                join conv in _db.Conversations
                    on cm1.ConversationId equals conv.Id
                join cm2 in _db.ConversationMembers
                    on cm1.ConversationId equals cm2.ConversationId

                where cm1.UserId == userId
                      && cm2.UserId != userId
                      && !conv.isGroup

                orderby cm1.ConversationId, cm1.UserId

                select new
                {
                    cm1.ConversationId,
                    conv.CreatedAt,
                    FriendId = cm2.UserId,
                    cm1.LastReadMessageId
                }
            ).ToListAsync();

            // 2.1 Wybieram tylko id konwersacji z wyników poprzedniego zapytania
            var conversationIds = conversations
                .Select(c => c.ConversationId)
                .ToList();

            // 2.2 Wybieram ostatnią wiadomość z danej konwersacji
            var lastMessages = await _db.Messages
                .Where(m => conversationIds.Contains(m.ConversationId))
                .GroupBy(m => m.ConversationId)
                .Select(g => g
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        ConversationId = m.ConversationId,
                        SenderId = m.SenderId,
                        Content = m.Content,
                        Type = m.Type,
                        CreatedAt = m.CreatedAt
                    })
                    .FirstOrDefault()
                )
                .ToListAsync();

            // 2.3 Zliczam ile mam nieprzeczytanych wiadomości w danej konwersacji
            var unreadCounts = await (
                from m in _db.Messages
                join cm in _db.ConversationMembers
                    on new { m.ConversationId, UserId = userId }
                    equals new { cm.ConversationId, cm.UserId }
                where conversationIds.Contains(m.ConversationId)
                      && (m.Id > cm.LastReadMessageId)
                group m by m.ConversationId into g
                select new
                {
                    ConversationId = g.Key,
                    Count = g.Count()
                }
            ).ToListAsync();

            // 3.1 Wybieram tylko id znajomych z wyników pierwszego zapytania
            var friendIds = conversations
                 .Select(c => c.FriendId)
                 .ToList();

            // 3.2 Znajduję znajomego w tabeli Users i zwracam o nim informacje
            var friends = await _db.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new MapUserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Latitude = u.Latitude,
                    Longitude = u.Longitude,
                    IsActive = u.IsActive,
                    ProfileImageUrl = u.ProfileImageUrl,
                    DefaultAvatarValue = u.DefaultAvatarValue
                })
                .ToListAsync();

            // 4. Łączę wyniki
            var friendsDict = friends.ToDictionary(f => f.Id);
            var lastMsgDict = lastMessages
                .Where(m => m != null)
                .ToDictionary(m => m!.ConversationId);
            var unreadDict = unreadCounts.ToDictionary(u => u.ConversationId);

            var result = conversations.Select(c =>
            {
                friendsDict.TryGetValue(c.FriendId, out var friend);
                lastMsgDict.TryGetValue(c.ConversationId, out var lastMsg);
                unreadDict.TryGetValue(c.ConversationId, out var unread);

                return new FriendCard
                {
                    ConversationId = c.ConversationId,
                    CreatedAt = c.CreatedAt,
                    Friend=friend!,
                    LastReadMessageId = c.LastReadMessageId,
                    LastMessage = lastMsg,
                    UnreadCount = unread?.Count ?? 0
                };
            }).ToList();

            return Ok(result);
        }

        // Usuwanie znajomego, jako parametr bierze id konwersacji, a nie id użytkownika!!!
        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpDelete("remove-friend/{friendId}")]
        public async Task<IActionResult> RemoveFriend([FromRoute]int? friendId)
        {
            if (friendId == null || friendId < 0) return BadRequest();
            var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(nameIdentifier, out int myId)) return Unauthorized("Brak dostępu");

            var relation = await _db.FriendRelations
                .Include(fr => fr.Conversation)
                .FirstOrDefaultAsync(fr => (fr.FriendId == myId && fr.UserId == friendId) || (fr.UserId == myId && fr.FriendId == friendId));

            if (relation == null) return NotFound("Nie znaleziono takiej relacji.");
            if(relation.Status == "Pending")return BadRequest("Nie jesteście jeszcze znajomymi");

            int? conversationId = relation.ConversationId;

            _db.FriendRelations.Remove(relation);
            _db.Conversations.Remove(relation.Conversation!);
            await _hub.Clients.User(friendId.ToString()!).SendAsync("RemoveFriend",conversationId);

            await _db.SaveChangesAsync();
            return Ok("Usunięto znajomego");
        }

        // Dodawanie znajomego
        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpPost("add-by-nick")]
        public async Task<IActionResult> AddByNick([FromQuery] string nick)
        {
            var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(nameIdentifier, out int myId)) return Unauthorized("Brak dostępu");

            if (string.IsNullOrWhiteSpace(nick)) return BadRequest("Nick nie może być pusty.");

            var friend = await _db.Users.FirstOrDefaultAsync(u => u.UserName.ToUpper() == nick.Trim().ToUpper());

            if (friend == null) return NotFound("Nie ma takiego użytkownika w bazie.");
            if (friend.Id == myId) return BadRequest("Nie możesz dodać samego siebie.");

            var exists = await _db.FriendRelations.AnyAsync(f =>
                (f.UserId == myId && f.FriendId == friend.Id) ||
                (f.UserId == friend.Id && f.FriendId == myId));

            if (exists) return Conflict("Już wysłałeś/aś zaproszenie.");

            var relation = new FriendRelation
            {
                UserId = myId,
                FriendId = friend.Id,
                Status = "Pending"
            };

            _db.FriendRelations.Add(relation);
            await _db.SaveChangesAsync();

            return Ok("Dodano znajomego!");
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPost("add-by-id/{id}")]
        public async Task<IActionResult> AddById(int id)
        {
            var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(nameIdentifier, out int myId)) return Unauthorized("Brak dostępu");

            if(id == myId)return BadRequest("Nie możesz dodać samego siebie.");

            var friend = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (friend == null) return NotFound("Nie ma takiego użytkownika w bazie.");

            var exists = await _db.FriendRelations.AnyAsync(f =>
                (f.UserId == myId && f.FriendId == friend.Id) ||
                (f.UserId == friend.Id && f.FriendId == myId));

            if (exists) return Conflict("Już wysłałeś/aś zaproszenie.");

            var relation = new FriendRelation
            {
                UserId = myId,
                FriendId = friend.Id,
                Status = "Pending"
            };

            _db.FriendRelations.Add(relation);
            await _db.SaveChangesAsync();

            return Ok("Dodano znajomego!");
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("Requests/Pending")]
        public async Task<IActionResult> PendingRequests()
        {
            var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(nameIdentifier, out int myId)) return Unauthorized("Brak dostępu");

            var users = await _db.FriendRelations
                .Where(f => (f.FriendId == myId || f.UserId == myId) && f.Status == "Pending")
                .Select(f => new FriendRequestDto
                {
                    Id = (f.UserId == myId ? f.Friend.Id : f.User.Id),
                    UserName = (f.UserId == myId ? f.Friend.UserName : f.User.UserName),
                    Latitude = (f.UserId == myId ? f.Friend.Latitude : f.User.Latitude),
                    Longitude = (f.UserId == myId ? f.Friend.Longitude : f.User.Longitude),
                    IsActive = (f.UserId == myId ? f.Friend.IsActive : f.User.IsActive),
                    DefaultAvatarValue = (f.UserId == myId ? f.Friend.DefaultAvatarValue : f.User.DefaultAvatarValue),
                    IsIncoming = (f.FriendId == myId)
                })
                .ToListAsync();
            return Ok(users);
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpPatch("Requests/Accept")]
        public async Task<IActionResult> AcceptRequest([FromBody]int friendId)
        {
            var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(nameIdentifier, out int myId)) return Unauthorized("Brak dostępu");

            var relation = await _db.FriendRelations.FirstOrDefaultAsync(cm => cm.FriendId == myId && cm.UserId == friendId && cm.Status == "Pending");
            if (relation == null) return NotFound("Nie znaleziono takiego zaproszenia");

            var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == myId);
            var friend = await _db.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            if (me == null || friend == null) return NotFound("Nie znaleziono takiego zaproszenia");


            FriendCards fc = await _fs.CreateRelationshipAsync(me, friend, relation);
            await _db.SaveChangesAsync();

            await _hub.Clients.User(myId.ToString()).SendAsync("RequestAccepted",fc.toMe);
            await _hub.Clients.User(friendId.ToString()).SendAsync("RequestAccepted",fc.toFriend);
            return Ok("Pomyślnie dodano znajomego");
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpDelete("Requests/Cancel")]
        public async Task<IActionResult> CancelRequest([FromBody] int friendId)
        {
            var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(nameIdentifier, out int myId)) return Unauthorized("Brak dostępu");

            var relation = await _db.FriendRelations.FirstOrDefaultAsync(cm => (
                (cm.FriendId == myId && cm.UserId == friendId) || (cm.FriendId == friendId && cm.UserId == myId)
            ) && cm.Status == "Pending");
            if (relation == null) return NotFound("Nie znaleziono takiego zaproszenia");

            _db.FriendRelations.Remove(relation);
            await _db.SaveChangesAsync();
            await _hub.Clients.User(friendId.ToString()).SendAsync("RequestCancelled",myId);
            return Ok("Pomyślnie anulowano/odrzucono zaproszenie do znajomych");
        }
    }
    
    class FriendRequestDto : MapUserDto
    {
        public bool IsIncoming { get; set; }
    }

    class FriendCard
    {
        public int ConversationId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public MapUserDto Friend { get; set; } = new MapUserDto();
        public int LastReadMessageId { get; set; }
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}

