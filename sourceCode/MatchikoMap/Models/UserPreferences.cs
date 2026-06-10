using System.Text.Json;
namespace MatchikoMap.Models
{ 
    public class UserPreferences
        {
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // 🔹 SINGLE
    public string? Gender { get; set; }
    public string? DayType { get; set; }
    public string? Status { get; set; }
    public string? Alcohol { get; set; }
    public string? Smoking { get; set; }
    public string? Intent { get; set; }

    // 🔹 MULTI (JSON jako string)
    public string? Hobby { get; set; }
    public string? Games { get; set; }
    public string? Books { get; set; }
    public string? Food { get; set; }
    public string? Drink { get; set; }
    public string? Music { get; set; }
    public string? FavoriteGames { get; set; }
    }
}

