using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OnBoarding.Data;
using OnBoarding.Models.POCOs;
using OnBoarding.Models;
using OnBoarding.Services;
using OnBoarding.Models.DTOs;

namespace OnBoarding.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly JWTHelper _jwtService;

        public LoginController(AppDbContext dbContext, JWTHelper jwtService)
        {
            _users = dbContext.GetCollection<User>("Users");
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTO03 dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response
                {
                    IsError = true,
                    Message = "Validation failed",
                    Data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var user = await _users.Find(u => u.Email == dto.Email && u.Password == dto.Password).FirstOrDefaultAsync();
            if (user == null)
            {
                return Unauthorized(new Response { IsError = true, Message = "Invalid email or password" });
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            // Set the token in an HTTP-only, Secure Cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,  // Prevent JavaScript from accessing it (XSS protection)
                Secure = true,    // Send only over HTTPS
                SameSite = SameSiteMode.Strict, // Protect against CSRF
                Expires = DateTime.UtcNow.AddDays(1) // Match JWT expiration
            };

            Response.Cookies.Append("userToken", token, cookieOptions);

            return Ok(new Response { IsError = false, Message = "Login successful" });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("userToken");

            return Ok(new Response { IsError = false, Message = "Logged out successfully" });
        }

    }
}
