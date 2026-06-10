using MatchikoMap.Models;

namespace MatchikoMap.Services.GroupConversationService
{
    public interface IGroupConversationService
    {
        Task<GroupConversationResult?> GetNearestConversationsForSpecifiedGameAsync(string gameName, User user);
        Task<List<GroupConversationDto>> GetAllConversationsForUserGamesAsync(int userId, User user);
    }
}