using MatchikoMap.Data;
using MatchikoMap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/location")]
    public class LocationController(UserManager<User> userManager, AppDbContext db) : ControllerBase
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly AppDbContext _db = db;

        // zapis lokalizacji użytkownika
        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPost]
        public async Task<IActionResult> SaveLocation([FromBody] LocationDto dto)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("Brak dostępu");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized("Brak dostępu");

            user.Latitude = dto.Latitude;
            user.Longitude = dto.Longitude;
            user.IsActive = true;

            await _db.SaveChangesAsync();
            return Ok();
        }

        // pobranie lokalizacji użytkowników z bazy danych
        [Authorize]
        [EnableRateLimiting("bucket-100")]
        [HttpGet]
        public async Task<IActionResult> GetUsersForMap()
        {
            var users = await _db.Users
                .Where(u => u.Latitude != null && u.Longitude != null)
                .Select(u => new MapUserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Latitude = u.Latitude!.Value,
                    Longitude = u.Longitude!.Value,
                    IsActive = u.IsActive,
                    ProfileImageUrl = u.ProfileImageUrl,
                    DefaultAvatarValue = u.DefaultAvatarValue
                })
                .ToListAsync();

            return Ok(users);
        }
    }

    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
