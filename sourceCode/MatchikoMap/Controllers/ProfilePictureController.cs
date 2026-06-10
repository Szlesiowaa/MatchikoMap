using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Services.ProfilePictureService;
using MatchikoMap.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using System.Security.Claims;


namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilePictureController(UserManager<User> userManager, AppDbContext db, IConfiguration config, IProfilePictureService pps) : Controller
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly AppDbContext _db = db;
        private readonly IConfiguration _config = config;
        private readonly IProfilePictureService _pps = pps;

        // wyslanie zdjecia profilowego
        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return Unauthorized();

            string? url;
            try
            {
                url = await _pps.UploadAsync(file, user.ProfileImageUrl);
            }
            catch (FileTooLargeException)
            {
                return BadRequest("Plik jest zbyt duży.");
            }
            catch (InvalidOperationException)
            {
                return BadRequest("Niepoprawny typ pliku.");
            }

            user.ProfileImageUrl = url;
            user.DefaultAvatarValue = null;

            await _db.SaveChangesAsync();

            return Ok(new { url = user.ProfileImageUrl });
        }

        // usuwanie zdjecia profilowego
        [Authorize]
        [EnableRateLimiting("bucket-15")]
        [HttpDelete]
        public async Task<IActionResult> DeleteProfileImage([FromQuery] string defaultImageType)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return Unauthorized();

            await _pps.DeleteAsync(user.ProfileImageUrl);

            user.ProfileImageUrl = null;
            user.DefaultAvatarValue = defaultImageType;

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
