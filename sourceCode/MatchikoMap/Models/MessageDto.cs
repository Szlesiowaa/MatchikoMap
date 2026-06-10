namespace MatchikoMap.Models
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string? Content { get; set; }
        public MessageType Type { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<MessageAttachmentDto> Attachments { get; set; } = [];
    }
}
