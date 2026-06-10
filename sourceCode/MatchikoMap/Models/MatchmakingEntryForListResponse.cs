namespace MatchikoMap.Models
{
    public class MatchmakingEntryForListResponse
    {
        public int MatchId { get; set; }
        public MapUserDto Creator { get; set; } = null!;
        public GameDto Game { get; set; } = null!;
        public DateTime ExpiringAt { get; set; }
        public string? Description { get; set; }
    }
}
