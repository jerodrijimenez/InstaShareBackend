using InstaShareAuthService.Models;
using InstaShareAuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstaShareAuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<User>> GetUsers()
        {
            var users = await _authService.GetUsers();
            return users;
        }

        [HttpGet("GetUserId/{id}")]
        [Authorize]
        public async Task<User> GetUserById([FromRoute] int id)
        {
            var user = await _authService.GetUserId(id);
            return user;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto userDto)
        {
            var user = await _authService.Register(userDto);
            return CreatedAtAction(nameof(Login), new { username = user.UserName }, user);
        }

        [HttpPost("login")]
        public string Login([FromBody] UserLoginDto userDto)
        {
            var token = _authService.Login(userDto);
            return token;
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _authService.Logout(userId);
            return Ok("Logged out successfully.");
        }

        [HttpPut("changePass")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string newPassword, string oldPassword)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _authService.UpdatePassword(userId, newPassword, oldPassword);
            return Ok("Password changed");
        }

        [HttpDelete("deleteAccount")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _authService.DeleteFile(userId);
            return Ok("Account deleted successfully.");
        }
    }
}
