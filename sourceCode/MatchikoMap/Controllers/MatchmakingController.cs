using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Services.MatchmakingService;
using MatchikoMap.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchmakingController(AppDbContext _db, IMatchmakingService _ms, IHubContext<ChatHub> _hub) : Controller
    {

        [Authorize]
        [EnableRateLimiting("bucket-300")]
        [HttpGet("Games/Search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Ok(new List<GameSearchResultDto>());

            var normalizedQuery = DatabaseSeeder.Normalize(query);

            var results = await _db.Games
                .Select(g => new
                {
                    Game = g,
                    Similarity = EF.Functions.TrigramsSimilarity(g.NormalizedName, normalizedQuery),
                    AcronymMatch = g.Acronym != null && g.Acronym.StartsWith(normalizedQuery)
                })
                .Where(x =>
                    EF.Functions.ILike(x.Game.NormalizedName, $"%{normalizedQuery}%") ||
                    x.Similarity > 0.2 ||
                    x.AcronymMatch
                )
                .OrderByDescending(x => x.AcronymMatch ? 1 : 0)
                .ThenByDescending(x => x.Similarity)
                .Take(20)
                .Select(x => new GameSearchResultDto
                {
                    Id = x.Game.Id,
                    Name = x.Game.Name,
                    Similarity = x.Similarity,
                    IconPath = x.Game.IconPath,
                    GridPath = x.Game.GridPath,
                    PosterPath = x.Game.PosterPath
                })
                .ToListAsync();

            return Ok(results);
        }


        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("Entries/My")]
        public async Task<IActionResult> MyEntries()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var response = await _ms.MyEntriesAsync(userId);
            if (response == null) return NoContent();
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPost("Entries/JoinOrCreate")]
        public async Task<IActionResult> JoinOrCreateEntry([FromBody] JoinOrCreateRequest dto)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized("Brak dostępu");

            if (user.Latitude == null || user.Longitude == null) return BadRequest("Brak danych o twojej lokalizacji");

            bool entryAlreadyCreated = await _db.MatchmakingEntries.AnyAsync(e => (e.CreatorUserId == userId || e.JoinerUserId == userId) && e.ExpiringAt > DateTime.UtcNow);
            if (entryAlreadyCreated) return Conflict("Uczestniczysz już w jednym zgłoszeniu. Zrezygnuj z niego, aby utworzyć kolejne.");

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == dto.GameId);
            if (game == null) return BadRequest("Nie znaleziono podanej gry.");

            IMatchmakingEntryResponse response = await _ms.JoinOrCreateEntryAsync(dto, game, user);
            if (response is MatchmakingEntryCreated) return Ok(response);
            else if (response is MatchmakingNotification notification)
            {
                await _hub.Clients.User(userId.ToString()).SendAsync("CompanionFound", response);
                if (userId == notification.Creator.Id)
                    await _hub.Clients.User(notification.Joiner.Id.ToString()).SendAsync("CompanionFound", response);
                else
                    await _hub.Clients.User(notification.Creator.Id.ToString()).SendAsync("CompanionFound", response);
                return NoContent();
            }
            return BadRequest();
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("Entries/List/{gameId?}")]
        public async Task<IActionResult> MatchmakingEntries(int? gameId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized("Brak dostępu");
            if (user.Latitude == null || user.Longitude == null) return BadRequest("Brak danych o twojej lokalizacji");

            List<MatchmakingEntryForListResponse> response = await _ms.NearbyEntriesAsync(user, gameId);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPatch("Entries/Accept/{matchId}")]
        public async Task<IActionResult> Accept(int matchId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            FriendCards? fc;
            try
            {
                fc = await _ms.AcceptAsync(userId, matchId);
            }
            catch (MatchmakingNotFoundException)
            {
                return NotFound();
            }
            catch (MatchmakingNotReadyException)
            {
                return BadRequest();
            }
            catch (AlreadyFriendsException ex)
            {
                await _hub.Clients.User(userId.ToString()).SendAsync("MatchmakingAcceptedButFriendsAlready", ex.ConversationId);
                await _hub.Clients.User(ex.FriendId.ToString()).SendAsync("MatchmakingAcceptedButFriendsAlready", ex.ConversationId);
                return Ok("Jesteście już znajomymi, matchmaking zakończony pomyślnie");
            }
            catch (Exception) { return BadRequest(); }


            if (fc == null) return Ok("Zaakceptowano matchmaking, oczekiwanie na drugą osobę.");

            await _hub.Clients.User(userId.ToString()).SendAsync("MatchmakingAccepted", fc.toMe);
            await _hub.Clients.User(fc.toMe.Friend.Id.ToString()).SendAsync("MatchmakingAccepted", fc.toFriend);

            return Ok("Matchmaking zakończony pomyślnie");
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpDelete("Entries/Reject/{matchId}")]
        public async Task<IActionResult> CancelOrReject(int matchId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            MatchmakingRejectResponse? response;
            try
            {
                response = await _ms.CancelOrRejectAsync(userId, matchId);
            }
            catch (MatchmakingNotFoundException) { return NotFound(); }
            catch(Exception) { Console.WriteLine("nie działa"); return BadRequest(); }

            if (response == null)
            {
                return NoContent();
            }
            else if(!response.AmICreator)
            {
                await _hub.Clients.User(response.CreatorUserId.ToString()).SendAsync("MatchmakingJoinerLeft",response.Data);
                return NoContent();
            }
            else
            {
                await _hub.Clients.User(response.JoinerUserId.ToString()).SendAsync("MatchmakingJoinerLeft",null);
                return Ok(response.Data);
            }
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPatch("Entries/Join/{matchId}")]
        public async Task<IActionResult> Join(int matchId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if(user == null) return NotFound();
            MatchmakingNotification response;
            try
            {
                response = await _ms.JoinEntryByMatchIdAsync(matchId, user);
            }
            catch(MatchmakingJoiningFailedException) { return BadRequest(); }
            await _hub.Clients.User(userId.ToString()).SendAsync("CompanionFound", response);
            if (userId == response.Creator.Id)
                await _hub.Clients.User(response.Joiner.Id.ToString()).SendAsync("CompanionFound", response);
            else
                await _hub.Clients.User(response.Creator.Id.ToString()).SendAsync("CompanionFound", response);
            return Ok();
        }

    }
    public class GameSearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Similarity { get; set; }
        public string? IconPath { get; set; }
        public string? GridPath { get; set; }
        public string? PosterPath { get; set; }
    }

    public class JoinOrCreateRequest
    {
        public int GameId { get; set; }
        public string? Description { get; set; }
        public MatchmakingEntryType Type { get; set; }
    }
}
