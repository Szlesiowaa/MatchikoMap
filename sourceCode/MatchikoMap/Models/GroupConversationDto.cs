namespace MatchikoMap.Models
{
    public class GroupConversationDto
    {
        public int ConversationId { get; set; }
        public int GameId { get; set; }
        public string? Name { get; set; }
        public bool IsGroup { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationName { get; set; }
        public double Distance { get; set; }
        public string? IconPath { get; set; }
    }
}
