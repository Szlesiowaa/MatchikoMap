namespace MatchikoMap.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool isGroup { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? GameId { get; set; } = null;
        public Game? Game { get; set; } = null;
        public ICollection<FriendRelation> FriendRelations { get; set; } = [];
        public ICollection<ConversationMember> Members { get; set; } = [];
        public ICollection<Message> Messages { get; set; } = [];
    }
}
