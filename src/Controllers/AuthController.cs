using DockerComposeManager.src.Models;
using DockerComposeManager.src.Services;
using Microsoft.AspNetCore.Mvc;

namespace DockerComposeManager.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var response = _authService.Login(request);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
} 
