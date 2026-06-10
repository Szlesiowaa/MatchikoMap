namespace MatchikoMap.Models
{
    public class MapUserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsActive { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? DefaultAvatarValue { get; set; }
    }
    public class MapUserDistDto : MapUserDto
    {
        public double Distance { get; set; }
    }
}
