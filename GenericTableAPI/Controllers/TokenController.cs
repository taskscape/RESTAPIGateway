using GenericTableAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GenericTableAPI.Controllers
{
    [Route("api/token")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TokenController(IConfiguration config)
        {
            _configuration = config;
        }

        [HttpPost]
        public async Task<IActionResult> Post(User _userData)
        {
            if (_userData?.UserName == null || _userData.Password == null) return BadRequest();
            var user = GetUser(_userData.UserName, _userData.Password);

            if (user != null)
            {
                return Ok(CreateToken(user));
            }

            return BadRequest("Invalid credentials");

        }

        private static User GetUser(string userName, string password)
        {
            return !string.IsNullOrEmpty(userName) ? new User() { UserName = "test", Password = "123" } : null;
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("Jwt:Key").Value));

            SigningCredentials signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: signingCredentials);

            string jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}