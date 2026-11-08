using BillMate.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BillMate.Data;
using Microsoft.EntityFrameworkCore;

namespace BillMate.Middleware
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _key;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ApplicationDbContext _db;

        public TokenService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _configuration = configuration;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")));
            _contextAccessor = httpContextAccessor;
            _db = db;
        }

        // Generate JWT Token
        public string GenerateJwtToken(bool isAdmin, User user)
        {
            if (user.Email == null || user.Id == null)
            {
                throw new ArgumentNullException("User email or ID cannot be null");
            }

            // Get subscription type for the user
            string subscriptionType = "None";
            if (user.GroupId != null)
            {
                var subscription = _db.Subscriptions
                    .Where(s => s.GroupId == user.GroupId && s.IsActive && (s.ExpiryTime == null || s.ExpiryTime > DateTime.UtcNow))
                    .OrderByDescending(s => s.StartTime)
                    .FirstOrDefault();
                if (subscription != null)
                {
                    subscriptionType = subscription.Type.ToString();
                }
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
            };
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMonths(Convert.ToInt32(Environment.GetEnvironmentVariable("JWT_EXPIRY"))),
                SigningCredentials = creds,
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            var encryptedToken = tokenHandler.WriteToken(token);
            _contextAccessor.HttpContext?.Response.Cookies.Append("token", encryptedToken, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                HttpOnly = false,
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.None,
            });
            return encryptedToken;
        }

        // Validation Method
        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            return tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
        }


        // Generate Refresh Token Method 
        //public string GenerateRefreshToken()
        //{
        //    var randomNumber = new byte[64];
        //    using (var numberGenerator = RandomNumberGenerator.Create())
        //    {
        //        numberGenerator.GetBytes(randomNumber);
        //    }
        //    return Convert.ToBase64String(randomNumber);
        //}
    }
}
