using InstaShareAuthService.Models;
using InstaShareAuthService.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace InstaShareAuthService.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _secretKey;
 
        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _secretKey = configuration["Jwt:Key"];
        }

        //Get all Users
        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        //Get one User
        public async Task<User> GetUserId(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        //Create a new Account
        public async Task<User> Register(UserRegistrationDto userdto)
        {
            if (_context.Users.Any(u => u.UserName == userdto.UserName))
                throw new Exception("User already exists.");

            var user = new User
            {
                UserName = userdto.UserName,
                UserEmail = userdto.UserEmail,
                UserPass = BCrypt.Net.BCrypt.HashPassword(userdto.UserPass)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        //Login with a created Acout
        public string Login(UserLoginDto userdto)
        {
            var user = _context.Users.SingleOrDefault(u => u.UserName == userdto.UserName);
            if (user == null || !BCrypt.Net.BCrypt.Verify(userdto.UserPass, user.UserPass))
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            return GenerateJwtToken(user);
        }

        //Generate the Auth Token
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "instashare",
                audience: "instashare_users",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //Logout the Account
        public async Task Logout(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found.");  


        }

        //Change the Password
        public async Task<User> UpdatePassword(int userId, string newPassword, string oldPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.UserPass))
                throw new Exception("Incorrect password.");

            user.UserPass = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return user;
        }

        //Delete user
        public async Task DeleteFile(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");


            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }


    }
}
