namespace MatchikoMap.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NormalizedName { get; set; } = null!;
        public string Acronym { get; set; } = null!;
        public string? IconPath { get; set; }
        public string? GridPath { get; set; }
        public string? PosterPath {  get; set; }
        public ICollection<UserFavouriteGames> FavouritedBy { get; set; } = new List<UserFavouriteGames>();
        public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    }
}
