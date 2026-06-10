using MatchikoMap.Data;
using MatchikoMap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatchikoMap.Utils
{
    [Authorize]
    public class ChatHub(AppDbContext db, UserManager<User> userManager) : Hub
    {
        private readonly AppDbContext _db = db;
        private readonly UserManager<User> _userManager = userManager;

        // Użytkownik dołącza do konwersacji
        public async Task JoinConversation(int conversationId)
        {
            if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Brak dostępu");
            bool? isGroup = await _db.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => c.isGroup)
                .FirstOrDefaultAsync();

            if (isGroup == null) throw new HubException("Brak dostępu");

            if (isGroup == false) {
                var isMember = await _db.ConversationMembers.AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
                if (!isMember) throw new HubException("Brak dostępu");
            }

             // Console.WriteLine("Użytkownik " + userId + " dołącza do konwersacji " + conversationId);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        // Użytkownik opuszcza konwersację
        public async Task LeaveConversation(int conversationId)
        {
            if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Brak dostępu");

            // Console.WriteLine("Użytkownik " + userId + " opuszcza konwersację " + conversationId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        // potwierdzenie dostarczenia wiadomości
        public async Task MessageRead(int conversationId)
        {
            if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Brak dostępu");
            var member = await _db.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
            if (member == null) return;
            member.LastReadMessageId = await _db.Messages
                .Where(m => m.ConversationId == conversationId)
                .MaxAsync(m => m.Id);
            await _db.SaveChangesAsync();
            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("MessageSeen", conversationId, userId);
        }

        // status że ktoś pisze
        public async Task UserTyping(int conversationId)
        {
            // if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Unauthorized");

            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("UserTyping", Context.User?.Identity?.Name ?? "Unknown");
        }
        
        public override async Task OnConnectedAsync()
        {
            // Console.WriteLine($"Użytkownik {Context.User?.Identity?.Name} połączył się z serwerem");
            if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Brak dostepu");

            var me = await _userManager.FindByIdAsync(Context.UserIdentifier) ?? throw new HubException("Brak dostepu");
            me.IsActive= true;
            await _db.SaveChangesAsync();

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
                    FriendId = cm2.UserId
                }
            ).ToListAsync();

            foreach(var c in conversations)
            {
                await Clients.User(c.FriendId.ToString()).SendAsync("UpdateStatus","ONLINE", c.ConversationId);
            }

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? e)
        {
            // Console.WriteLine($"Użytkownik {Context.User?.Identity?.Name} rozłączył się z serwerem");

            if (!int.TryParse(Context.UserIdentifier, out var userId)) throw new HubException("Brak dostępu");

            var me = await _userManager.FindByIdAsync(Context.UserIdentifier) ?? throw new HubException("Brak dostepu");
            me.IsActive = false;
            await _db.SaveChangesAsync();

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
                    FriendId = cm2.UserId
                }
            ).ToListAsync();

            foreach (var c in conversations)
            {
                await Clients.User(c.FriendId.ToString()).SendAsync("UpdateStatus", "OFFLINE", c.ConversationId);
            }

            await base.OnDisconnectedAsync(e);
        }

        /*
        public async Task MessageEdited(int messageId, string newContent)
        {

        }

        public async Task MessageDeleted(int messageId)
        {
  
        }
        */
    }

}
