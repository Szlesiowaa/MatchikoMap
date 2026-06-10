namespace MatchikoMap.Models
{
    public class MessageAttachmentDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
        public long Size { get; set; }
        public string Url { get; set; } = null!;
    }
}
