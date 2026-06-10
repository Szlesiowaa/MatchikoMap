namespace MatchikoMap.Models
{
    public class AzureBlobStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ProfileImagesContainer { get; set; } = string.Empty;
        public string AttachmentsContainer { get; set; } = string.Empty;
    }
}
