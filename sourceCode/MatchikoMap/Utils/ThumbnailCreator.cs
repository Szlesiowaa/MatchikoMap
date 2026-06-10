using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.AspNetCore.Http;

namespace MatchikoMap.Utils
{
    public class ThumbnailCreator
    {
    /// <summary>
    /// Tworzy miniaturę 1:1 z przesłanego pliku IFormFile i zapisuje w trzech różnych rozmiarach (100x100, 300x300 i 600x600).
    /// </summary>
    public static async Task CreateThumbnails(IFormFile file, string dirPath, string fileName)
    {
            using var image = await Image.LoadAsync(file.OpenReadStream());
            string outputPath;

            // przycinanie do kwadratu z centrum
            int cropSize = Math.Min(image.Width, image.Height);
            var cropRectangle = new Rectangle(
                (image.Width - cropSize) / 2,
                (image.Height - cropSize) / 2,
                cropSize,
                cropSize
            );
            image.Mutate(x => x.Crop(cropRectangle));
            var cropped = image.Clone(x => { });

            cropped.Mutate(x => x
                .Resize(600, 600)
            );

            outputPath = Path.Combine(dirPath, "600x600");
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            outputPath = Path.Combine(dirPath, "600x600", fileName);
            await cropped.SaveAsync(outputPath, new JpegEncoder());

            cropped = image.Clone(x => { });
            cropped.Mutate(x => x.Resize(300, 300));

            outputPath = Path.Combine(dirPath, "300x300");
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            outputPath = Path.Combine(dirPath, "300x300", fileName);
            await cropped.SaveAsync(outputPath, new JpegEncoder());

            cropped = image.Clone(x => { });
            cropped.Mutate(x => x.Resize(100, 100));

            outputPath = Path.Combine(dirPath, "100x100");
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            outputPath = Path.Combine(dirPath, "100x100", fileName);
            await cropped.SaveAsync(outputPath, new JpegEncoder());
        }
    }
}
