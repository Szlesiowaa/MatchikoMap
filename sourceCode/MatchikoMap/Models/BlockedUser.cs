namespace MatchikoMap.Models
{
    public class BlockedUser
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int BlockedUserId { get; set; }
        public User Blocked { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
