using Microsoft.AspNetCore.Identity;
namespace MatchikoMap.Models
{
    public class User:IdentityUser<int>
    {
        public new string UserName
        {
            get => base.UserName!;
            set => base.UserName = value;
        }
        
        public new string Email
        {
            get => base.Email!;
            set => base.Email = value;
        }
        public new string? PasswordHash{
            get => base.PasswordHash;
            set => base.PasswordHash = value;
        }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Tags { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string? DefaultAvatarValue { get; set; }
        public bool LocationPermission { get; set; } = true;
        public ICollection<ConversationMember> ConversationMembers { get; set; } = [];
        public ICollection<Message> Messages { get; set; } = [];
        public ICollection<FriendRelation> FriendsInitiated { get; set; } = [];
        public ICollection<FriendRelation> FriendsReceived { get; set; } = [];
        public ICollection<BlockedUser> UsersBlocked { get; set; } = [];
        public ICollection<BlockedUser> BlockedByUsers { get; set; } = [];
        public UserSettings Settings { get; set; } = null!;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double RadiusKm { get; set; } = 30;
        public ICollection<UserFavouriteGames> FavouriteGames { get; set; } = [];

        public UserPreferences? Preferences { get; set; }

    }
}
