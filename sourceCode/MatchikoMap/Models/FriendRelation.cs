namespace MatchikoMap.Models
{
    public class FriendRelation
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int FriendId { get; set; }
        public User Friend { get; set; } = null!;
        public string Status { get; set; } = "Pending";
        public int? ConversationId { get; set; }
        public Conversation? Conversation { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
