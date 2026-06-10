using MatchikoMap.Models;

namespace MatchikoMap.Services.FriendshipsService
{
    public interface IFriendshipsService
    {
        Task<FriendCards> CreateRelationshipAsync(User me, User friend, FriendRelation? relation);
    }
}
