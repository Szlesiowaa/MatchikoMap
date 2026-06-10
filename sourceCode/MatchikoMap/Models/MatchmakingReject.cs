namespace MatchikoMap.Models
{
    public class MatchmakingReject
    {
        public int MatchId { get; set; }
        public MatchmakingEntry Entry { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
