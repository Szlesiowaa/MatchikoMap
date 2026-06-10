namespace MatchikoMap.Models
{
    public class UserFavouriteGames
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
    }
}
