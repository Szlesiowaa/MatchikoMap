using System.Text.Json.Serialization;

namespace MatchikoMap.Models
{
    public class MatchmakingEntry
    {
        public int MatchId { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
        public int CreatorUserId { get; set; }
        public User CreatorUser { get; set; } = null!;
        public MatchmakingEntryUserStatus CreatorStatus { get; set; }
        public int? JoinerUserId { get; set; }
        public User? JoinerUser { get; set; }
        public MatchmakingEntryUserStatus? JoinerStatus { get; set; }
        public DateTime ExpiringAt { get; set; }
        public string? Description { get; set; }
        public MatchmakingEntryType Type { get; set; }

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchmakingEntryUserStatus
    {
        Searching,
        Pending,
        Accepted
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchmakingEntryType
    {
        Auto,
        Manual
    }
}
