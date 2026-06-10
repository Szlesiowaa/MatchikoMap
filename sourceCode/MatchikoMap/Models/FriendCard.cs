namespace MatchikoMap.Models
{
    public class FriendCard
    {
        public int ConversationId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public MapUserDto Friend { get; set; } = new MapUserDto();
        public int LastReadMessageId { get; set; }
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
