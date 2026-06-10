namespace MatchikoMap.Models
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        

        public List<int>? HobbyIds { get; set; }

        public List<int>? GameTypes { get; set; }
        public List<int>? Games { get; set; }
        public List<int>? BookTypes { get; set; }
        public List<int>? Food { get; set; }
        public List<int>? Drinks { get; set; }
        public List<int>? Music { get; set; }
        public List<int>? favoriteGames { get; set; }

        public int? Gender { get; set; }
        public int? DayType { get; set; }
        public int? Activity { get; set; }
        public int? Alcohol { get; set; }
        public int? Smoking { get; set; }
        public int? Intentions { get; set; }
    }
}
