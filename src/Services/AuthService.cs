using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DockerComposeManager.src.Models;
using Microsoft.IdentityModel.Tokens;

namespace DockerComposeManager.src.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginResponse Login(LoginRequest request)
        {
            var username = _configuration["AppSettings:Username"];
            var password = _configuration["AppSettings:Password"];

            if (request.Username == username && request.Password == password)
            {
                var token = GenerateJwtToken(request.Username);
                return new LoginResponse
                {
                    Success = true,
                    Token = token
                };
            }

            return new LoginResponse
            {
                Success = false,
                Error = "用户名或密码错误"
            };
        }

        private string GenerateJwtToken(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AppSettings:Secret"] ?? "default-secret-key"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "DockerComposeManager",
                audience: "DockerComposeManagerUsers",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 
