using MatchikoMap.Data;
using MatchikoMap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController(AppDbContext _db) : ControllerBase
    {
        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpPost]
        public async Task<IActionResult> Search([FromBody] SearchRequestDto dto)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostêpu");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized("Brak dostêpu");
            if (user.Latitude == null || user.Longitude == null) return BadRequest("Brak lokalizacji u¿ytkownika");

            var myPrefs = await _db.UserPreferences.FirstOrDefaultAsync(up => up.UserId == userId);
            if (myPrefs == null) dto.SimilarToMe = false;

            var usersPreferences = await _db.UserPreferences
                .Include(up => up.User)
                .Where(up => up.User.Latitude != null && up.User.Longitude != null && up.UserId != userId)
                .ToListAsync();

            var result = new List<MapUserDistDto>();

            foreach (var up in usersPreferences)
            {
                double distance = CalculateDistance(
                    user.Latitude.Value,
                    user.Longitude.Value,
                    up.User.Latitude!.Value,
                    up.User.Longitude!.Value
                );

                if (distance > dto.RangeKm) continue;

                if (dto.SimilarToMe)
                {
                    int score = 0;

                    if (up.Gender == myPrefs!.Gender) score++;

                    if (up.DayType == myPrefs!.DayType) score++;

                    if (up.Status == myPrefs!.Status) score++;

                    if (up.Intent == myPrefs.Intent) score++;

                    if (score < 2) continue;
                }

                // =========================
                // SINGLE FILTERS
                // =========================

                // plec
                if (!string.IsNullOrWhiteSpace(dto.Gender))
                {
                    Console.WriteLine("DTO GENDER: " + dto.Gender);
                    Console.WriteLine("USER GENDER: " + up.Gender);

                    if (!string.Equals(up.Gender?.Trim(),dto.Gender?.Trim(),StringComparison.OrdinalIgnoreCase)) continue;
                }

                // alko
                if (!string.IsNullOrEmpty(dto.Alcohol) && !string.Equals(up.Alcohol?.Trim(), dto.Alcohol?.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                // palenie
                if (!string.IsNullOrEmpty(dto.Smoking) && !string.Equals(up.Smoking?.Trim(), dto.Smoking?.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                // nie wiem co
                if (!string.IsNullOrEmpty(dto.Intent) && up.Intent != dto.Intent) continue;

                // =========================
                // MULTI FILTERS
                // =========================

                if (dto.Hobby != null && dto.Hobby.Count != 0)
                {
                    var userHobby = up.Hobby != null ? JsonSerializer.Deserialize<List<string>>(up.Hobby) ?? [] : [];

                    if (!userHobby.Any(x => dto.Hobby.Contains(x))) continue;
                }

                if (dto.Games != null && dto.Games.Count != 0)
                {
                    var userGames = up.Games != null ? JsonSerializer.Deserialize<List<string>>(up.Games) ?? [] : [];
                    if (!userGames.Any(x => dto.Games.Contains(x))) continue;
                }

                if (dto.Books != null && dto.Books.Count != 0)
                {
                    var userBooks = up.Books != null ? JsonSerializer.Deserialize<List<string>>(up.Books) ?? [] : [];
                    if (!userBooks.Any(x => dto.Books.Contains(x))) continue;
                }

                if (dto.Food != null && dto.Food.Count != 0)
                {
                    var userFood = up.Food != null ? JsonSerializer.Deserialize<List<string>>(up.Food) ?? [] : [];

                    if (!userFood.Any(x => dto.Food.Contains(x))) continue;
                }

                if (dto.Drink != null && dto.Drink.Count != 0)
                {
                    var userDrink = up.Drink != null ? JsonSerializer.Deserialize<List<string>>(up.Drink) ?? [] : [];

                    if (!userDrink.Any(x => dto.Drink.Contains(x))) continue;
                }

                if (dto.Music != null && dto.Music.Count != 0)
                {
                    var userMusic = up.Music != null ? JsonSerializer.Deserialize<List<string>>(up.Music) ?? [] : [];

                    if (!userMusic.Any(x => dto.Music.Contains(x))) continue;
                }

                if (dto.FavoriteGames != null && dto.FavoriteGames.Count != 0)
                {
                    var userFav = up.FavoriteGames != null ? JsonSerializer.Deserialize<List<string>>(up.FavoriteGames) ?? [] : [];

                    if (!userFav.Any(x => dto.FavoriteGames.Contains(x))) continue;
                }

                result.Add(new MapUserDistDto
                {
                    Id = up.UserId,
                    UserName = up.User.UserName,
                    Latitude = up.User.Latitude,
                    Longitude = up.User.Longitude,
                    IsActive = up.User.IsActive,
                    DefaultAvatarValue = up.User.DefaultAvatarValue,
                    ProfileImageUrl = up.User.ProfileImageUrl,
                    Distance = Math.Round(distance, 1),
                }); 
            }

            return Ok(result);
        }

        // Haversine
        static private double CalculateDistance(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            double R = 6371;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        static private double DegreesToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }

    public class SearchRequestDto
    {
        public int RangeKm { get; set; }

        public bool SimilarToMe { get; set; }

        public List<string>? Hobby { get; set; }

        public List<string>? Games { get; set; }

        public List<string>? Music { get; set; }

        public List<string>? FavoriteGames { get; set; }

        public string? Gender { get; set; }
        public string? Alcohol { get; set; }

        public string? Smoking { get; set; }
        public string? Intent { get; set; }

        public List<string>? Books { get; set; }

        public List<string>? Food { get; set; }

        public List<string>? Drink { get; set; }


    }
}