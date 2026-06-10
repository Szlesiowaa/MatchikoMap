namespace MatchikoMap.Models
{
    public class UserSettings
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int SearchRadiusKm { get; set; } = 10;
        public string ProfileVisibility { get; set; } = "public";
        public string LocationVisibility { get; set; } = "friends";
        public string LastSeenVisibility { get; set; } = "friends";
    }
}
