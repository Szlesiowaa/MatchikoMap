namespace MatchikoMap.Models
{
    public class MatchmakingRejectResponse
    {
        public bool AmICreator { get; set; }
        public int CreatorUserId { get; set; }
        public int JoinerUserId { get; set; }
        public MatchmakingEntryCreated Data { get; set; } = null!;

    }
}
