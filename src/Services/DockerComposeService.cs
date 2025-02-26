using System.Diagnostics;
using System.Text.Json;
using DockerComposeManager.src.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DockerComposeManager.src.Services
{
    public class DockerComposeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DockerComposeService> _logger;
        private readonly List<string> _basePaths;

        public DockerComposeService(IConfiguration configuration, ILogger<DockerComposeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // 获取配置的基础目录，支持多个目录（逗号分隔）
            var basePathConfig = _configuration["AppSettings:DockerComposeBasePath"] ?? "/opt/docker";
            _basePaths = basePathConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            
            _logger.LogInformation($"Docker Compose base paths: {string.Join(", ", _basePaths)}");
        }

        public Dictionary<string, string> FindComposeFiles()
        {
            var composeFiles = new Dictionary<string, string>();
            
            foreach (var basePath in _basePaths)
            {
                try
                {
                    if (!Directory.Exists(basePath))
                    {
                        _logger.LogWarning($"Base path does not exist: {basePath}");
                        continue;
                    }
                    
                    foreach (var dir in Directory.GetDirectories(basePath))
                    {
                        var dirName = Path.GetFileName(dir);
                        var composeFile = Path.Combine(dir, "docker-compose.yml");
                        if (File.Exists(composeFile))
                        {
                            // 使用完整路径作为键，以避免不同基础目录中的同名目录冲突
                            var serviceKey = $"{Path.GetFileName(basePath)}/{dirName}";
                            composeFiles[serviceKey] = composeFile;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error finding docker-compose files in {basePath}");
                }
            }
            
            return composeFiles;
        }

        public Dictionary<string, object>? ReadComposeFile(string serviceDir)
        {
            var composeFiles = FindComposeFiles();
            if (!composeFiles.ContainsKey(serviceDir))
            {
                return null;
            }

            try
            {
                var yaml = File.ReadAllText(composeFiles[serviceDir]);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                return deserializer.Deserialize<Dictionary<string, object>>(yaml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading docker-compose file for {serviceDir}");
                return null;
            }
        }

        public bool WriteComposeFile(string serviceDir, Dictionary<string, object> data)
        {
            var composeFiles = FindComposeFiles();
            if (!composeFiles.ContainsKey(serviceDir))
            {
                return false;
            }

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yaml = serializer.Serialize(data);
                File.WriteAllText(composeFiles[serviceDir], yaml);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing docker-compose file for {serviceDir}");
                return false;
            }
        }

        public string GetServiceStatus(string serviceDir, string serviceName)
        {
            var composeFiles = FindComposeFiles();
            if (!composeFiles.ContainsKey(serviceDir))
            {
                return "未知";
            }

            try
            {
                var composeDir = Path.GetDirectoryName(composeFiles[serviceDir]) ?? "";
                
                // 使用 docker-compose ps 命令获取状态
                var processInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"cd {composeDir} && docker-compose ps {serviceName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return "错误";
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return "错误";
                }

                if (output.Contains("Exit") || output.Contains("exited"))
                {
                    return "已停止";
                }
                else if (output.Contains("Up") || output.Contains("running"))
                {
                    return "运行中";
                }
                else if (string.IsNullOrWhiteSpace(output) || !output.Contains(serviceName))
                {
                    return "未配置";
                }
                else
                {
                    return "未知";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting service status for {serviceDir}/{serviceName}");
                return "错误";
            }
        }

        public Dictionary<string, ServiceDirectory> GetServicesInfo(SearchRequest search)
        {
            var composeFiles = FindComposeFiles();
            var servicesInfo = new Dictionary<string, ServiceDirectory>();
            
            // 首先过滤服务目录
            var filteredDirs = new Dictionary<string, string>();
            foreach (var (serviceDir, composeFile) in composeFiles)
            {
                // 检查目录搜索条件
                bool dirMatches = string.IsNullOrEmpty(search.DirSearch) || 
                                  serviceDir.ToLower().Contains(search.DirSearch.ToLower());
                
                // 检查服务名称搜索条件 - 这里将 nameSearch 也应用于 serviceDir
                bool nameMatches = string.IsNullOrEmpty(search.NameSearch) || 
                                   serviceDir.ToLower().Contains(search.NameSearch.ToLower());
                
                // 如果任一条件匹配，则包含此目录
                if (dirMatches || nameMatches)
                {
                    filteredDirs[serviceDir] = composeFile;
                }
            }
            
            // 然后处理每个匹配的目录
            foreach (var (serviceDir, composeFile) in filteredDirs)
            {
                var composeData = ReadComposeFile(serviceDir);
                if (composeData != null && composeData.ContainsKey("services") && composeData["services"] is Dictionary<object, object> services)
                {
                    var serviceDirectory = new ServiceDirectory
                    {
                        Path = composeFile,
                        Services = new Dictionary<string, ServiceInfo>()
                    };

                    foreach (var service in services)
                    {
                        var serviceName = service.Key.ToString() ?? "";
                        if (string.IsNullOrEmpty(serviceName)) continue;
                        
                        var serviceConfig = service.Value as Dictionary<object, object>;
                        if (serviceConfig == null) continue;

                        var image = serviceConfig.ContainsKey("image") ? serviceConfig["image"].ToString() ?? "" : "";
                        
                        // 检查镜像名称是否匹配
                        if (!string.IsNullOrEmpty(search.ImageSearch) && !image.ToLower().Contains(search.ImageSearch.ToLower()))
                        {
                            continue;
                        }
                        
                        var version = "";
                        if (image.Contains(':'))
                        {
                            version = image.Split(':')[1];
                        }
                        
                        // 检查版本是否匹配
                        if (!string.IsNullOrEmpty(search.VersionSearch) && !version.ToLower().Contains(search.VersionSearch.ToLower()))
                        {
                            continue;
                        }
                        
                        // 只有在需要时才获取服务状态
                        string status;
                        var hasSearch = !string.IsNullOrEmpty(search.DirSearch) || 
                                       !string.IsNullOrEmpty(search.NameSearch) || 
                                       !string.IsNullOrEmpty(search.ImageSearch) || 
                                       !string.IsNullOrEmpty(search.VersionSearch) || 
                                       !string.IsNullOrEmpty(search.StatusSearch);
                                       
                        if (!string.IsNullOrEmpty(search.StatusSearch) || !hasSearch)
                        {
                            status = GetServiceStatus(serviceDir, serviceName);
                            
                            // 检查状态是否匹配
                            if (!string.IsNullOrEmpty(search.StatusSearch) && !status.ToLower().Contains(search.StatusSearch.ToLower()))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // 如果不需要检查状态，设置为待查询
                            status = "待查询";
                        }
                        
                        serviceDirectory.Services[serviceName] = new ServiceInfo
                        {
                            Image = image,
                            Version = version,
                            Status = status
                        };
                    }
                    
                    // 只有当服务目录中有匹配的服务时才添加到结果中
                    if (serviceDirectory.Services.Count > 0)
                    {
                        servicesInfo[serviceDir] = serviceDirectory;
                    }
                }
            }
            
            return servicesInfo;
        }

        public bool UpdateVersion(string serviceDir, string serviceName, string newVersion)
        {
            var composeData = ReadComposeFile(serviceDir);
            if (composeData == null || !composeData.ContainsKey("services"))
            {
                return false;
            }

            var services = composeData["services"] as Dictionary<object, object>;
            if (services == null || !services.ContainsKey(serviceName))
            {
                return false;
            }

            var service = services[serviceName] as Dictionary<object, object>;
            if (service == null || !service.ContainsKey("image"))
            {
                return false;
            }

            var image = service["image"].ToString() ?? "";
            if (string.IsNullOrEmpty(image))
            {
                return false;
            }

            if (image.Contains(':'))
            {
                var baseImage = image.Split(':')[0];
                service["image"] = $"{baseImage}:{newVersion}";
            }
            else
            {
                service["image"] = $"{image}:{newVersion}";
            }

            return WriteComposeFile(serviceDir, composeData);
        }

        public bool ExecuteDockerComposeCommand(string serviceDir, string serviceName, string command)
        {
            var composeFiles = FindComposeFiles();
            if (!composeFiles.ContainsKey(serviceDir))
            {
                return false;
            }

            try
            {
                var composeDir = Path.GetDirectoryName(composeFiles[serviceDir]) ?? "";
                var processInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"cd {composeDir} && docker-compose {command} {serviceName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return false;
                }

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing docker-compose command for {serviceDir}/{serviceName}");
                return false;
            }
        }
    }
} 
