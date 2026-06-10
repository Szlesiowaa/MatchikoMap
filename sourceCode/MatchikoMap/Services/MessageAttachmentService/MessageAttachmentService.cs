using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MatchikoMap.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MatchikoMap.Services.MessageAttachmentService
{
    public class MessageAttachmentService : IMessageAttachmentService
    {
        private readonly BlobContainerClient _container;

        public MessageAttachmentService(IConfiguration config)
        {
            var blobService = new BlobServiceClient(config["AzureBlobStorage:ConnectionString"]);
            _container = blobService.GetBlobContainerClient(config["AzureBlobStorage:AttachmentsContainer"]);
        }

        public async Task<MessageAttachment> UploadAsync(IFormFile file, int messageId, CancellationToken ct = default)
        {
            const long maxSize = 20 * 1024 * 1024;

            if (file == null || file.Length == 0) throw new ArgumentException("Niepoprawny plik");

            if (file.Length > maxSize) throw new ArgumentOutOfRangeException(nameof(file), "Plik za duży (max 20MB)");

            var contentType = file.ContentType?.ToLowerInvariant();

            var isImage = contentType?.StartsWith("image/") == true;
            var isVideo = contentType?.StartsWith("video/") == true;

            if (!isImage && !isVideo) throw new FormatException("Nieobsługiwany format pliku");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobName = $"{messageId}/{fileName}";
            var blobClient = _container.GetBlobClient(blobName);

            if (isImage)
            {
                await using var input = file.OpenReadStream();

                using var image = await Image.LoadAsync(input, ct);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1920, 1920)
                }));

                await using var output = new MemoryStream();

                await image.SaveAsJpegAsync(output, new JpegEncoder
                {
                    Quality = 80
                }, ct);

                output.Position = 0;

                await blobClient.UploadAsync(
                    output,
                    new BlobHttpHeaders
                    {
                        ContentType = "image/jpeg"
                    },
                    cancellationToken: ct);
            }
            else
            {
                await using var stream = file.OpenReadStream();

                await blobClient.UploadAsync(
                    stream,
                    new BlobHttpHeaders
                    {
                        ContentType = contentType
                    },
                    cancellationToken: ct);
            }

            return new MessageAttachment
            {
                MessageId = messageId,
                BlobName = blobName,
                Type = contentType!,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };
        }

        public async Task DeleteAsync(string blobName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                return;

            var blobClient = _container.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        }

        public string GenerateReadSas(string blobName, TimeSpan lifetime)
        {
            var blobClient = _container.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _container.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}