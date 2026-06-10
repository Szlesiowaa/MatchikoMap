namespace MatchikoMap.Services.ProfilePictureService
{
    public interface IProfilePictureService
    {
        Task DeleteAsync(string? imageName, CancellationToken cancellationToken = default);
        Task<string> UploadAsync(IFormFile file, string? oldImageName, CancellationToken cancellationToken = default);
    }
}