using MatchikoMap.Data;
using MatchikoMap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController(UserManager<User> userManager, AppDbContext db) : Controller
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly AppDbContext _db = db;

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("{id?}")]
        public async Task<IActionResult> GetProfile([FromRoute] string? id)
        {
            string? userId;

            if (id != null) userId = id;
            else
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized("Brak dostępu");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Nie znaleziono użytkownika");
            var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);

            return Ok(new  {
                user.Id,
                user.UserName,
                user.Description,
                user.Tags,
                user.ProfileImageUrl,
                user.DefaultAvatarValue,

                // 🔹 SINGLE
                gender = prefs?.Gender,
                dayType = prefs?.DayType,
                status = prefs?.Status,
                alcohol = prefs?.Alcohol,
                smoking = prefs?.Smoking,
                intent = prefs?.Intent,

                // 🔹 MULTI
                hobby = prefs?.Hobby != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.Hobby)
    : [],
                games = prefs?.Games != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.Games)
    : [],
                books = prefs?.Books != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.Books)
    : [],
                food = prefs?.Food != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.Food)
    : [],
                drink = prefs?.Drink != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.Drink)
    : [],
                music = prefs?.Music != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.Music)
    : [],
                favoriteGames = prefs?.FavoriteGames != null
    ? JsonSerializer.Deserialize<List<string>>(prefs.FavoriteGames)
    : [],
            });
        }

        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfilePreferencesDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user == null) return Unauthorized();

            if (
                string.IsNullOrWhiteSpace(dto.UserName) ||
                dto.UserName.Length < 4 || dto.UserName.Length > 20 || !Regex.IsMatch(dto.UserName, @"^[a-zA-Z0-9._-]+$") ||
                (dto.Tags != null && dto.Tags.Length > 100) ||
                (dto.Description != null && dto.Description.Length > 3000)
            )
            {
                return BadRequest("Niepoprawne dane.");
            }

            user.UserName = dto.UserName;
            user.Tags = dto.Tags;
            user.Description = dto.Description;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

           

            var prefs = await _db.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (prefs == null)
            {
                prefs = new UserPreferences { UserId = user.Id };
                _db.UserPreferences.Add(prefs);
            }


            prefs.Gender = dto.Gender ?? "";
            prefs.DayType = dto.DayType ?? "";
            prefs.Status = dto.Status ?? "";
            prefs.Alcohol = dto.Alcohol ?? "";
            prefs.Smoking = dto.Smoking ?? "";
            prefs.Intent = dto.Intent ?? "";


            prefs.Hobby = JsonSerializer.Serialize(dto.Hobby ?? []);
            prefs.Games = JsonSerializer.Serialize(dto.Games ?? []);
            prefs.Books = JsonSerializer.Serialize(dto.Books ?? []);
            prefs.Food = JsonSerializer.Serialize(dto.Food ?? []);
            prefs.Drink = JsonSerializer.Serialize(dto.Drink ?? []);
            prefs.Music = JsonSerializer.Serialize(dto.Music ?? []);
            prefs.FavoriteGames = JsonSerializer.Serialize(dto.FavoriteGames ?? []);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            return Ok(new { message = "Profil zaktualizowany!" });
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("whoami")]
        public async Task<IActionResult> WhoAmI()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("Brak dostępu");

            return Ok(new { id = user.Id });
        }

        [Authorize]
        [EnableRateLimiting("fixed-6")]
        [HttpPatch("Report/{id}")]
        public async Task<IActionResult> Report(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            if (userId == id) return Conflict();
            var post = await _db.Users.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();
            return Ok();
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet("LocationPermission")]
        public async Task<IActionResult> LocationPermission()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (me == null) return Unauthorized();
            var result = me.LocationPermission;
            return Ok(new {locationPermission = result});
        }

        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpPatch("LocationPermission")]
        public async Task<IActionResult> ToggleLocationPermission()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, out var userId)) return Unauthorized("Brak dostępu");
            var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (me == null) return Unauthorized();
            var result = !me.LocationPermission;
            me.LocationPermission = !me.LocationPermission;
            await _db.SaveChangesAsync();
            return Ok(new { locationPermission = result });
        }
    }
    public class ProfileDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Tags { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? DefaultAvatarValue { get; set; } = null;

    }
    public class ProfilePreferencesDto
    {
        public string UserName { get; set; } = null!;
        public string? Description { get; set; }
        public string? Tags { get; set; }

        public string? Gender { get; set; }
        public string? DayType { get; set; }
        public string? Status { get; set; }
        public string? Alcohol { get; set; }
        public string? Smoking { get; set; }
        public string? Intent { get; set; }

        public List<string>? Hobby { get; set; }
        public List<string>? Games { get; set; }
        public List<string>? Books { get; set; }
        public List<string>? Food { get; set; }
        public List<string>? Drink { get; set; }
        public List<string>? Music { get; set; }
        public List<string>? FavoriteGames { get; set; }
    }
}
