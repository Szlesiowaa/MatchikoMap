using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MatchikoMap.Models;
using MatchikoMap.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MatchikoMap.Services.ProfilePictureService
{
    public class ProfilePictureService : IProfilePictureService
    {
        private readonly BlobContainerClient _containerClient;

        public ProfilePictureService(IConfiguration configuration)
        {
            var connectionString = configuration["MediaStorage:ConnectionString"];

            var containerName = configuration["MediaStorage:ProfileImagesContainer"];

            var blobServiceClient = new BlobServiceClient(connectionString);

            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadAsync(IFormFile file, string? oldImageName, CancellationToken cancellationToken = default)
        {
            const long maxFileSizeInBytes = 5 * 1024 * 1024;

            if (file == null || file.Length == 0) throw new ArgumentException("Niepoprawny plik.");

            if (file.Length > maxFileSizeInBytes) throw new FileTooLargeException();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension)) throw new InvalidOperationException("Niepoprawny typ pliku.");

            var fileName = $"{Guid.NewGuid()}.webp";

            await using var inputStream = file.OpenReadStream();

            using var image = await Image.LoadAsync(inputStream, cancellationToken);

            await UploadOriginalImageAsync(image, $"original/{fileName}", cancellationToken);
            await UploadResizedImageAsync(image, $"100x100/{fileName}", 100, cancellationToken);
            await UploadResizedImageAsync(image, $"300x300/{fileName}", 300, cancellationToken);
            await UploadResizedImageAsync(image, $"600x600/{fileName}", 600, cancellationToken);
            try
            {
                if (!string.IsNullOrWhiteSpace(oldImageName)) await DeleteAsync(oldImageName, cancellationToken);
            }
            catch (MatchikoMapException)
            {
            }

            return fileName;
        }

        public async Task DeleteAsync(string? imageName, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(imageName)) return;

            var paths = new[]
            {
                $"original/{imageName}",
                $"100x100/{imageName}",
                $"300x300/{imageName}",
                $"600x600/{imageName}"
            };

            foreach (var path in paths)
            {
                try
                {
                    var blobClient = _containerClient.GetBlobClient(path);

                    await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                }
                catch
                {
                    throw new MatchikoMapException("Operacja usuwania się nie powiodła.");
                }
            }
        }

        private async Task UploadOriginalImageAsync(Image image, string blobPath, CancellationToken cancellationToken)
        {
            await using var output = new MemoryStream();

            await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 85 }, cancellationToken);

            output.Position = 0;

            var blobClient = _containerClient.GetBlobClient(blobPath);

            await blobClient.UploadAsync(
                output,
                new BlobHttpHeaders
                {
                    ContentType = "image/webp"
                },
                cancellationToken: cancellationToken);
        }

        private async Task UploadResizedImageAsync(Image sourceImage, string blobPath, int size, CancellationToken cancellationToken)
        {
            using var clone = sourceImage.Clone(x =>
                x.Resize(new ResizeOptions
                {
                    Size = new Size(size, size),
                    Mode = ResizeMode.Crop
                }));

            await UploadOriginalImageAsync(clone, blobPath, cancellationToken);
        }
    }
}