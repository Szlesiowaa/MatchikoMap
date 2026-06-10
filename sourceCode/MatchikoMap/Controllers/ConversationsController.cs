using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Services.GroupConversationService;
using MatchikoMap.Services.MessageAttachmentService;
using MatchikoMap.Utils;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.CodeDom;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Claims;

namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationsController(AppDbContext _db, IHubContext<ChatHub> _hub, IGroupConversationService _gcs, IMessageAttachmentService _mas) : Controller
    {
        // tlumaczy z id użytkownika na czas
        public static readonly ConcurrentDictionary<int, DateTime> _lastMessage = new();

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetMessages(int conversationId,[FromQuery]int? beforeMessageId)
        {
            int limit = 50;
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var myId)) return Unauthorized("Brak dostępu");

            bool? isGroup = await _db.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => c.isGroup)
                .FirstOrDefaultAsync();

            if (isGroup == null) return Unauthorized("Brak dostępu");

            ConversationMember? member = null;
            if(isGroup == false)member = await _db.ConversationMembers.FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == myId);

            if (member == null && isGroup==false) return Forbid("Nie jesteś uczestnikiem tej konwersacji");

            var query = _db.Messages.Where(m => m.ConversationId == conversationId);

            if (beforeMessageId.HasValue)
            {
                var beforeMessage = await _db.Messages
                    .Where(m => m.Id == beforeMessageId.Value)
                    .Select(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                query = query.Where(m => m.CreatedAt < beforeMessage);
            }

            var messagesRaw = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender.UserName,
                    m.Content,
                    m.Type,
                    m.CreatedAt,
                    Attachment = m.Attachments.Select(a => new
                    {
                        a.Id,
                        a.Type,
                        a.Size,
                        a.BlobName
                    }).FirstOrDefault()
                })
                .ToListAsync();

            var messages = messagesRaw.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.SenderName!,
                Content = m.Content,
                Type = m.Type,
                CreatedAt = m.CreatedAt,
                Attachments = m.Attachment != null
                    ?
                      [
                          new MessageAttachmentDto
                          {
                              Id = m.Attachment.Id,
                              Type = m.Attachment.Type,
                              Size = m.Attachment.Size,
                              Url = _mas.GenerateReadSas(m.Attachment.BlobName, TimeSpan.FromHours(1))
                          }
                      ]
                    : []
            }).ToList();

            if (beforeMessageId == null && messages.Count>0 && isGroup == false)
            {
                member!.LastReadMessageId = messages[0].Id;
                await _hub.Clients.GroupExcept($"conversation_{conversationId}", myId.ToString()).SendAsync("MessageSeen", conversationId, myId);
                await _db.SaveChangesAsync();
            }

            return Ok(messages);
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPost("{conversationId}")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromForm] IncomingMessageDto incomingMessage)
        {
            if (incomingMessage.ConnectionId == null) return Unauthorized("Brak połączenia SignalR.");
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var senderId)) return Unauthorized("Brak dostępu");

            if (_lastMessage.TryGetValue(senderId, out var last))
            {
                if ((DateTime.UtcNow - last).TotalMilliseconds < 1000)return StatusCode(StatusCodes.Status429TooManyRequests);
            }
            _lastMessage.AddOrUpdate(senderId, DateTime.UtcNow, (_, __) => DateTime.UtcNow);

            if (String.IsNullOrWhiteSpace(incomingMessage.Content) && (incomingMessage.File == null || incomingMessage.File.Length == 0))
            {
                return NoContent();
            }

            bool? isGroup = await _db.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => c.isGroup)
                .FirstOrDefaultAsync();

            if (isGroup == null) return Unauthorized("Brak dostępu");

            bool isMember = false;
            if(isGroup == false) isMember = await _db.ConversationMembers.AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == senderId);

            if (isMember == false && isGroup == false) return Unauthorized("Brak dostępu");

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = incomingMessage.Content,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            MessageAttachmentDto? attachmentToSend= null;

            if (incomingMessage.File != null && incomingMessage.File.Length > 0)
            {
                try
                {
                    var attachment = await _mas.UploadAsync(incomingMessage.File,message.Id);

                    _db.MessageAttachments.Add(attachment);

                    if (attachment.Type.StartsWith("image/")) message.Type = MessageType.Image;
                    else message.Type = MessageType.Video;

                    await _db.SaveChangesAsync();

                    attachmentToSend = new MessageAttachmentDto
                    {
                        Id = attachment.Id,
                        Type = attachment.Type,
                        Size = attachment.Size,
                        Url = _mas.GenerateReadSas(attachment.BlobName, TimeSpan.FromHours(1))
                    };
                }
                catch (ArgumentOutOfRangeException)
                {
                    _db.Messages.Remove(message);
                    await _db.SaveChangesAsync();

                    return StatusCode(413, "Plik za duży (max 20MB)");
                }
                catch (FormatException)
                {
                    _db.Messages.Remove(message);
                    await _db.SaveChangesAsync();

                    return StatusCode(415, "Nieobsługiwany format pliku");
                }
                catch (Exception)
                {
                    _db.Messages.Remove(message);
                    await _db.SaveChangesAsync();

                    return BadRequest("Błąd przetwarzania pliku");
                }
            }

            // broadcast do uczestników
            var senderName = User.FindFirstValue(ClaimTypes.Name)!;
            var dto = new MessageDto
            {
                Id = message.Id,
                ConversationId = conversationId,
                SenderId = message.SenderId,
                SenderName = senderName,
                Content = message.Content,
                Type = message.Type,
                CreatedAt = message.CreatedAt,
                Attachments = attachmentToSend != null ? [attachmentToSend]: []
            };

            if (isGroup==false)
            {
                var participants = await _db.ConversationMembers
                    .Where(x => x.ConversationId == conversationId)
                    .Select(x => x.UserId)
                    .ToListAsync();

                await _hub.Clients.Users(participants.Select(x => x.ToString())).SendAsync("ConversationUpdated", new
                {
                    senderId,
                    conversationId,
                    content = message.Content,
                    createdAt = message.CreatedAt,
                    type = message.Type
                });
            }

            await _hub.Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", dto);

            return Ok();
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPut("{conversationId}/Messages/{messageId}")]
        public async Task<IActionResult> EditMessage(int conversationId, int messageId, [FromBody] EditMessageRequest request)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var myId))
                return Unauthorized();

            var message = await _db.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);

            if (message == null)
                return NotFound("Wiadomość nie istnieje");

            if (message.SenderId != myId)
                return Forbid("Możesz edytować tylko własne wiadomości");

            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Treść nie może być pusta");

            message.Content = request.Content;

            await _db.SaveChangesAsync();

            // Powiadomienie innych użytkowników przez SignalR
            await _hub.Clients.Group($"conversation_{conversationId}")
                .SendAsync("MessageEdited", messageId, request.Content);

            return Ok();
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpDelete("{conversationId}/Messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int conversationId, int messageId, CancellationToken ct)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var myId)) return Unauthorized();

            var message = await _db.Messages
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken: ct);

            if (message == null) return NotFound("Wiadomość nie istnieje");
            if (message.SenderId != myId) return Forbid("Możesz usuwać tylko własne wiadomości");

            foreach (var attachment in message.Attachments)
            {
                await _mas.DeleteAsync(attachment.BlobName, ct);
            }
            _db.MessageAttachments.RemoveRange(message.Attachments);
            _db.Messages.Remove(message);
            await _db.SaveChangesAsync(ct);

            // Powiadomienie SignalR o usunięciu
            await _hub.Clients.Group($"conversation_{conversationId}").SendAsync("MessageDeleted", messageId, cancellationToken: ct);

            return NoContent();
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("Group")]
        public async Task<IActionResult> GroupConversations([FromQuery] string gameName)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            if (string.IsNullOrEmpty(gameName)) return BadRequest("Nie znaleziono gry");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized("Brak dostępu");

            if (user.Latitude == null || user.Longitude == null) return BadRequest("Brak danych o twojej lokalizacji");

            GroupConversationResult? result = await _gcs.GetNearestConversationsForSpecifiedGameAsync(gameName, user);
            if (result == null) return BadRequest("Nie znaleziono gry.");
            if (result.Conversations.Count == 0) return NotFound("Nie znaleziono żadnych konwersacji w pobliżu.");

            return Ok(result);
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpGet("Group/Favourite")]
        public async Task<IActionResult> GetFavouriteChats()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized("Brak dostępu");

            if (user.Latitude == null || user.Longitude == null) return BadRequest("Brak danych o twojej lokalizacji.");

            List<GroupConversationDto> result = await _gcs.GetAllConversationsForUserGamesAsync(userId, user);

            return Ok(result);
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPatch("Group/Favourite")]
        public async Task<IActionResult> ToggleFavourite([FromQuery] string gameName)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            if (string.IsNullOrEmpty(gameName)) return BadRequest("Nie znaleziono gry");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized("Brak dostępu");

            if (user.Latitude == null || user.Longitude == null) return BadRequest("Brak danych o twojej lokalizacji.");

            var game = await _db.Games.FirstOrDefaultAsync(x => x.Name == gameName);
            if (game == null) return BadRequest("Nie znaleziono podanej gry.");


            var matchingGame = await _db.UserFavouriteGames.FirstOrDefaultAsync(ufg => (ufg.UserId == userId && ufg.GameId == game.Id));

            if (matchingGame == null)
            {
                int gamesSavedN = _db.UserFavouriteGames
                    .Where(ufg => ufg.UserId == userId)
                    .Count();

                if (gamesSavedN >= 8) return Forbid("Dozwolone jest zapisanie max 8 gier w ulubionych.");

                _db.UserFavouriteGames.Add(new UserFavouriteGames()
                {
                    UserId = userId,
                    GameId = game.Id
                });
            }
            else
            {
                _db.UserFavouriteGames.Remove(matchingGame);
            }
            await _db.SaveChangesAsync();

            GroupConversationResult? conversations = await _gcs.GetNearestConversationsForSpecifiedGameAsync(game.Name, user);

            return Ok(new
            {
                addedToFavourite = matchingGame is null,
                response = conversations
            });
        }

        [Authorize]
        [EnableRateLimiting("fixed-6")]
        [HttpPatch("Messages/Report/{id}")]
        public async Task<IActionResult> Report(int id)
        {
            var post = await _db.Messages.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();
            return Ok();
        }

        public class IncomingMessageDto
        {
            public int ConversationId { get; set; }
            public string? Content { get; set; }
            public IFormFile? File { get; set; }
            public string? ConnectionId { get; set; }
        }
        public class EditMessageRequest
        {
            public string? Content { get; set; }
        }
    }

}
