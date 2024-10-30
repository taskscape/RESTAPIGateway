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
        private readonly List<AuthUser>? _users = [];
        public TokenController(IConfiguration config)
        {
            _configuration = config;
            _configuration.GetSection("JwtSettings:Users").Bind(_users);
        }

        [HttpPost]
        public async Task<IActionResult> Post(User userData)
        {
            if (userData?.UserName == null || userData.Password == null) return BadRequest();

            if (ValidateCredentials(userData.UserName, userData.Password))
            {
                return Ok(CreateToken(_users.Where(x => x.Username == userData.UserName).FirstOrDefault()));
            }

            return BadRequest("Invalid credentials");
        }

        private bool ValidateCredentials(string userName, string password)
        {
            return _users.Any(user => user.Username == userName && user.Password == password);
        }
        private string CreateToken(AuthUser user)
        {
            List<Claim> claims = [new Claim(ClaimTypes.Name, user.Username)];

            if (!string.IsNullOrEmpty(user.Role))
                claims.Add(new Claim(ClaimTypes.Role, user.Role));

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