using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Services.EmailService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;


namespace MatchikoMap.Controllers
{
    [ApiController]
    [Route("api")]
    public class LoginController(UserManager<User> _userManager, SignInManager<User> _signInManager, IConfiguration _config, AppDbContext _db, IBackgroundEmailQueue _eq) : ControllerBase
    {
        [EnableRateLimiting("sliding-10")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Wszystkie pola są wymagane.");
            if (dto.UserName.Length < 4 || dto.UserName.Length > 20 || !Regex.IsMatch(dto.UserName, @"^[a-zA-Z0-9._-]+$")) return BadRequest("Niepoprawna nazwa użytkownika.");

            var existingUserByName = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUserByName != null) return Conflict("Podana nazwa użytkownika już istnieje.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null) return Conflict("Podany email już istnieje.");
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                EmailConfirmed = false,
                DefaultAvatarValue = "domyslne.png"
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { errors });
            }

            await _userManager.AddToRoleAsync(user, "User");

            user.Preferences = new UserPreferences { UserId = user.Id };
            await _db.SaveChangesAsync();

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

            var baseUrl = _config["AppSettings:BaseUrl"];
            var confirmationLink = $"{baseUrl}/confirm-email.html?userId={user.Id}&token={encodedToken}";

            var htmlMessage = $@"
                <!DOCTYPE html><html lang='pl'> <head> <meta charset='UTF-8' /> </head> <body style=' margin:0; padding:40px 20px; background:#020617; font-family:Orbitron,Arial,sans-serif; color:#e5e7eb;'> <div style=' max-width:600px; margin:0 auto; background:#020617; border-radius:16px; border:1px solid #3d1b46; padding:40px; box-shadow:0 0 40px rgba(168,85,247,0.35);'> <h1 style=' margin-top:0; margin-bottom:30px; color:#a855f7; text-align:center; letter-spacing:3px; font-size:30px; font-weight:800; text-transform:uppercase;'> MatchikoMap </h1><p style='color:#cbd5e1; font-size:20px; font-weight:bold; line-height:1.8; margin-bottom:18px;'> 
                  Dziękujemy za rejestrację.
                </p> <p style=' color:#94a3b8; font-size:14px; line-height:1.8; margin-bottom:35px;'> Aby aktywować konto i uzyskać dostęp do platformy,<br>potwierdź swój adres e-mail. </p> <div style=' text-align:center; margin-bottom:35px;'> <a href='{confirmationLink}' style=' display:inline-block; padding:15px 34px; background:#a855f7; color:#020617; text-decoration:none; border-radius:10px; font-weight:700; font-size:14px; letter-spacing:2px; text-transform:uppercase; box-shadow:0 0 18px rgba(168,85,247,0.8);'> Potwierdź konto </a> </div> <hr style=' margin:40px 0 20px; border:none; border-top:1px solid #334155;'> <p style=' margin:0; color:#64748b; font-size:12px; text-align:center; line-height:1.7;'> Jeśli to nie Ty tworzyłeś konto, możesz zignorować tę wiadomość. </p> </div> </body> </html>""";

            _eq.QueueEmail(user.Email, "Witamy na MatchikoMap!", htmlMessage);

            return Ok("Zarejestrowano. Sprawdź email.");
        }

        [EnableRateLimiting("fixed-20")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized("Niepoprawne dane logowania");

            if (await _userManager.IsLockedOutAsync(user)) return Unauthorized("Konto zablokowane na 15 minut.");

            if (!user.EmailConfirmed) return Unauthorized(new { emailConfirmed = false, message = "Email nie został potwierdzony" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (!result.Succeeded) return Unauthorized("Niepoprawne dane logowania");

            var accessToken = await GenerateToken(user);
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });


            Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { emailConfirmed = true, message = "Logged in" });
        }

        [EnableRateLimiting("bucket-100")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue)) return Unauthorized("Brak dostępu");
            var refreshToken = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);

            // refreshToken nie działa
            if (refreshToken == null) return Unauthorized("Brak dostępu");
            if (refreshToken.Expires < DateTime.UtcNow) return Unauthorized(new { error = "SESSION_EXPIRED" });
            var newAccessToken = await GenerateToken(refreshToken.User);
            refreshToken.Token = GenerateRefreshToken();
            refreshToken.Expires = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            Response.Cookies.Append("accessToken", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok();
        }

        [EnableRateLimiting("fixed-20")]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue))
            {
                var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);
                if (token != null)
                {
                    _db.RefreshTokens.Remove(token);
                    await _db.SaveChangesAsync();
                }
            }
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Wylogowano pomyślnie" });
        }

        [EnableRateLimiting("sliding-10")]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.userId.ToString());
            if (user == null) return BadRequest("Nieprawidłowy użytkownik");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded) return BadRequest("Błąd potwierdzenia");

            return Ok("Email potwierdzony");
        }

        [EnableRateLimiting("bucket-100")]
        [HttpGet("google-redirection")]
        public IActionResult GoogleRedirection()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = $"/api/google-login" }, GoogleDefaults.AuthenticationScheme);
        }

        [EnableRateLimiting("fixed-20")]
        [HttpGet("google-login")]
        public async Task<IActionResult> GoogleLogin()
        {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!result.Succeeded) return Unauthorized();
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            if (email == null) return BadRequest("Nie udało się pobrać emaila");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Rejestracja nowego użytkownika
                user = new User { Email = email, UserName = email.Split('@')[0], EmailConfirmed = true, DefaultAvatarValue = "domyslne.png" };
                await _userManager.CreateAsync(user);
            }
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            var token = await GenerateToken(user);
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            //access token
            HttpContext.Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            //refresh token
            Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });
            return Redirect("/mapa.html");
        }

        [NonAction]
        public async Task<string> GenerateToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.UserName),
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // podpisujemy 👤🤝🖥️

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpireMinutes"]!)),
                signingCredentials: creds
              );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [NonAction]
        static private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }
    }
}