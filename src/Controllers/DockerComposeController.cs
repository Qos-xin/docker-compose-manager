using DockerComposeManager.src.Models;
using DockerComposeManager.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DockerComposeManager.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DockerComposeController : ControllerBase
    {
        private readonly DockerComposeService _dockerComposeService;
        private readonly ILogger<DockerComposeController> _logger;

        public DockerComposeController(DockerComposeService dockerComposeService, ILogger<DockerComposeController> logger)
        {
            _dockerComposeService = dockerComposeService;
            _logger = logger;
        }

        [HttpGet("services")]
        public IActionResult GetServices([FromQuery] SearchRequest search)
        {
            try
            {
                // 如果 nameSearch 有值但 dirSearch 没有，将 nameSearch 的值赋给 dirSearch
                if (!string.IsNullOrEmpty(search.NameSearch) && string.IsNullOrEmpty(search.DirSearch))
                {
                    search.DirSearch = search.NameSearch;
                }
                
                var services = _dockerComposeService.GetServicesInfo(search);
                
                // 计算匹配的服务总数
                int totalServices = 0;
                foreach (var serviceDir in services.Values)
                {
                    totalServices += serviceDir.Services.Count;
                }
                
                return Ok(new { services, totalServices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services");
                return StatusCode(500, new { error = "获取服务列表失败" });
            }
        }

        [HttpPost("refresh-status")]
        public IActionResult RefreshStatus([FromBody] ServiceActionRequest request)
        {
            if (string.IsNullOrEmpty(request.ServiceDir) || string.IsNullOrEmpty(request.ServiceName))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "服务目录和服务名称不能为空" });
            }

            var status = _dockerComposeService.GetServiceStatus(request.ServiceDir, request.ServiceName);
            return Ok(status);
        }

        [HttpPost("update-version")]
        public IActionResult UpdateVersion([FromBody] ServiceActionRequest request)
        {
            if (string.IsNullOrEmpty(request.ServiceDir) || string.IsNullOrEmpty(request.ServiceName) || string.IsNullOrEmpty(request.NewVersion))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "服务目录、服务名称和版本号不能为空" });
            }

            var success = _dockerComposeService.UpdateVersion(request.ServiceDir, request.ServiceName, request.NewVersion);
            if (success)
            {
                // 更新版本后，拉取新镜像并重新部署
                var pullSuccess = _dockerComposeService.ExecuteDockerComposeCommand(request.ServiceDir, request.ServiceName, "pull");
                var upSuccess = _dockerComposeService.ExecuteDockerComposeCommand(request.ServiceDir, request.ServiceName, "up -d");
                
                if (pullSuccess && upSuccess)
                {
                    return Ok(new ApiResponse { Success = true, Message = $"服务 {request.ServiceDir}/{request.ServiceName} 的版本已更新为 {request.NewVersion} 并已重新部署" });
                }
                else
                {
                    return Ok(new ApiResponse { Success = true, Message = $"版本已更新为 {request.NewVersion}，但重新部署失败" });
                }
            }
            
            return BadRequest(new ApiResponse { Success = false, Message = "更新版本失败" });
        }

        [HttpPost("restart")]
        public IActionResult RestartService([FromBody] ServiceActionRequest request)
        {
            if (string.IsNullOrEmpty(request.ServiceDir) || string.IsNullOrEmpty(request.ServiceName))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "服务目录和服务名称不能为空" });
            }

            var success = _dockerComposeService.ExecuteDockerComposeCommand(request.ServiceDir, request.ServiceName, "restart");
            if (success)
            {
                return Ok(new ApiResponse { Success = true, Message = $"服务 {request.ServiceDir}/{request.ServiceName} 已重启" });
            }
            
            return BadRequest(new ApiResponse { Success = false, Message = "重启服务失败" });
        }

        [HttpPost("stop")]
        public IActionResult StopService([FromBody] ServiceActionRequest request)
        {
            if (string.IsNullOrEmpty(request.ServiceDir) || string.IsNullOrEmpty(request.ServiceName))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "服务目录和服务名称不能为空" });
            }

            var success = _dockerComposeService.ExecuteDockerComposeCommand(request.ServiceDir, request.ServiceName, "stop");
            if (success)
            {
                return Ok(new ApiResponse { Success = true, Message = $"服务 {request.ServiceDir}/{request.ServiceName} 已停止" });
            }
            
            return BadRequest(new ApiResponse { Success = false, Message = "停止服务失败" });
        }

        [HttpPost("start")]
        public IActionResult StartService([FromBody] ServiceActionRequest request)
        {
            if (string.IsNullOrEmpty(request.ServiceDir) || string.IsNullOrEmpty(request.ServiceName))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "服务目录和服务名称不能为空" });
            }

            var success = _dockerComposeService.ExecuteDockerComposeCommand(request.ServiceDir, request.ServiceName, "up -d");
            if (success)
            {
                return Ok(new ApiResponse { Success = true, Message = $"服务 {request.ServiceDir}/{request.ServiceName} 已启动" });
            }
            
            return BadRequest(new ApiResponse { Success = false, Message = "启动服务失败" });
        }
    }
} 
