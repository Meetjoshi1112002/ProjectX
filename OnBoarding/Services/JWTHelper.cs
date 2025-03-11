using Microsoft.IdentityModel.Tokens;
using OnBoarding.Models.POCOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace OnBoarding.Services
{
    public class JWTHelper
    {
        private readonly IConfiguration _config;

        public JWTHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));

            var claims = new List<Claim>
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("userRole", user.Role.ToString()), // Assuming Role is stored in User model
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(jwtSettings["ExpiryInDays"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
