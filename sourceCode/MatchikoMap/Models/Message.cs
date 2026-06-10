namespace MatchikoMap.Models
{
    public enum MessageType
    {
        Text=0,
        Image=1,
        Video=2
    }
    public class Message
    {
        public int Id { get; set; }

        // FK do Conversation
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        // FK do User (nadawca)
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public MessageType Type { get; set; }
        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }
        public ICollection<MessageAttachment> Attachments { get; set; } = [];
    }
}
