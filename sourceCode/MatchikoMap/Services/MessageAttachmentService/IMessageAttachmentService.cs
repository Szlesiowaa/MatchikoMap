using MatchikoMap.Models;

namespace MatchikoMap.Services.MessageAttachmentService
{
    public interface IMessageAttachmentService
    {
        Task<MessageAttachment> UploadAsync(IFormFile file, int messageId, CancellationToken ct = default);
        Task DeleteAsync(string blobName, CancellationToken ct);
        string GenerateReadSas(string blobName, TimeSpan lifetime);
    }
}