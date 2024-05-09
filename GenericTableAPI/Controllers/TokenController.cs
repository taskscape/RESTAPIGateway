using GenericTableAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        public async Task<IActionResult> Post(User userData)
        {
            if (userData?.UserName == null || userData.Password == null) return BadRequest();

            if (ValidateCredentials(userData.UserName, userData.Password))
            {
                return Ok(CreateToken(userData));
            }

            return BadRequest("Invalid credentials");
        }

        private bool ValidateCredentials(string userName, string password)
        {
            string? confUsername = _configuration.GetSection("JwtSettings:Username").Value;
            string? confPassword = _configuration.GetSection("JwtSettings:Password").Value;

            return confUsername == userName && confPassword == password;
        }
        private string CreateToken(User user)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin")
            ];

            SymmetricSecurityKey key = new(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("JwtSettings:Key").Value));

            SigningCredentials signingCredentials = new(key, SecurityAlgorithms.HmacSha512Signature);

            JwtSecurityToken token = new(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: signingCredentials);

            string jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}