namespace MatchikoMap.Models
{
    public class MessageAttachment
    {
        public int Id { get; set; }

        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public string BlobName { get; set; } = null!;

        public string Type { get; set; } = null!;

        public long Size { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
