namespace MatchikoMap.Models
{
    public interface IMatchmakingEntryResponse
    {
        string OperationType { get; set; }
    }

    public class MatchmakingEntryCreated : IMatchmakingEntryResponse
    {
        public int MatchId { get; set; }
        public string OperationType { get; set; } = "Created";
        public MatchmakingEntryUserStatus Status { get; set; }
        public DateTime ExpiringAt { get; set; }
        public GameDto Game { get; set; } = null!;
    }
    public class MatchmakingEntryJoined : MatchmakingEntryCreated
    {
        public MatchmakingEntryJoined(){
            OperationType = "Joined";
        }

        public MapUserDto User { get; set; } = null!;
        public string? Description { get; set; }
        public MatchmakingEntryType Type { get; set; }
    }
    public class MatchmakingNotification:IMatchmakingEntryResponse
    {
        public string OperationType { get; set; } = "Notificate";
        public int MatchId { get; set; }
        public MapUserDto Creator { get; set; } = null!;
        public MapUserDto Joiner { get; set; } = null!;
        public GameDto Game { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime ExpiringAt { get; set; }
    }
}
