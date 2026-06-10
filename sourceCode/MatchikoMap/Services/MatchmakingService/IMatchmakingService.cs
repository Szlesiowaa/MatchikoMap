using MatchikoMap.Controllers;
using MatchikoMap.Models;

namespace MatchikoMap.Services.MatchmakingService
{
    public interface IMatchmakingService
    {
        Task<MatchmakingEntryCreated> CreateNewEntryAsync(JoinOrCreateRequest dto, Game game, int userId);
        Task<IMatchmakingEntryResponse> JoinOrCreateEntryAsync(JoinOrCreateRequest dto, Game game, User user);
        Task<List<MatchmakingEntryForListResponse>> NearbyEntriesAsync(User user, int? gameId);
        Task<IMatchmakingEntryResponse?> MyEntriesAsync(int userId);
        Task<FriendCards?> AcceptAsync(int userId, int matchId);
        Task<MatchmakingRejectResponse?> CancelOrRejectAsync(int userId, int matchId);
        Task<MatchmakingNotification> JoinEntryByMatchIdAsync(int matchId, User user);
    }
}