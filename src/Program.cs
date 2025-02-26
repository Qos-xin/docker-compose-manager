using System.Text;
using DockerComposeManager.src.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddSingleton<DockerComposeService>();
builder.Services.AddSingleton<AuthService>();

// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "DockerComposeManager",
            ValidAudience = "DockerComposeManagerUsers",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Secret"] ?? "default-secret-key"))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run(); 
