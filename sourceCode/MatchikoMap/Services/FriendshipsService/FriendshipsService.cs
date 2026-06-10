using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Utils;
using Microsoft.AspNetCore.SignalR;

namespace MatchikoMap.Services.FriendshipsService
{
    public class FriendshipsService(AppDbContext _db):IFriendshipsService
    {
        public async Task<FriendCards> CreateRelationshipAsync(User me, User friend, FriendRelation? relation)
        {
            var conversation = new Conversation
            {
                isGroup = false,
                Members =
                [
                     new() { User = me },
                     new() { User = friend }
                ]
            };

            relation ??= new FriendRelation
            {
                User = me,
                Friend = friend,
            };

            relation.Status = "Accepted";
            relation.CreatedAt = DateTime.UtcNow;
            relation.Conversation = conversation;

            _db.Conversations.Add(conversation);
            _db.FriendRelations.Update(relation);
            await _db.SaveChangesAsync();


            FriendCard toMe = new()
            {
                ConversationId = conversation.Id,
                Friend = new MapUserDto
                {
                    Id = friend.Id,
                    UserName = friend.UserName,
                    Latitude = friend.Latitude,
                    Longitude = friend.Longitude,
                    IsActive = friend.IsActive,
                    ProfileImageUrl = friend.ProfileImageUrl,
                    DefaultAvatarValue = friend.DefaultAvatarValue
                },
                LastReadMessageId = 0,
                UnreadCount = 0
            };
            FriendCard toFriend = new()
            {
                ConversationId = conversation.Id,
                Friend = new MapUserDto
                {
                    Id = me.Id,
                    UserName = me.UserName,
                    Latitude = me.Latitude,
                    Longitude = me.Longitude,
                    IsActive = me.IsActive,
                    ProfileImageUrl = me.ProfileImageUrl,
                    DefaultAvatarValue = me.DefaultAvatarValue
                },
                LastReadMessageId = 0,
                UnreadCount = 0
            };

            return new FriendCards { toMe = toMe, toFriend = toFriend };
        }
    }
}
