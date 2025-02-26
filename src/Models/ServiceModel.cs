namespace DockerComposeManager.src.Models
{
    public class ServiceInfo
    {
        public string Image { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ServiceDirectory
    {
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, ServiceInfo> Services { get; set; } = new Dictionary<string, ServiceInfo>();
    }

    public class SearchRequest
    {
        public string DirSearch { get; set; } = string.Empty;
        public string NameSearch { get; set; } = string.Empty;
        public string ImageSearch { get; set; } = string.Empty;
        public string VersionSearch { get; set; } = string.Empty;
        public string StatusSearch { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    public class ServiceActionRequest
    {
        public string ServiceDir { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string? NewVersion { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 
