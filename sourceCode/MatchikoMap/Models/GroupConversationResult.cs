namespace MatchikoMap.Models
{
    public class GroupConversationResult
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<GroupConversationDto> Conversations { get; set; } = [];
        public int GameId { get; set; }
    }
}
