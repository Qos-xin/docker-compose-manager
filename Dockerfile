FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 复制项目文件并还原依赖
COPY src/*.csproj ./src/
WORKDIR /app/src
RUN dotnet restore

# 复制所有文件并构建
WORKDIR /app
COPY . .
WORKDIR /app/src
RUN dotnet publish -c Release -o /app/publish

# 构建运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 安装 bash 和其他必要的工具
RUN apt-get update && apt-get install -y \
    bash \
    curl \
    && rm -rf /var/lib/apt/lists/*

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# 暴露端口
EXPOSE 80

ENTRYPOINT ["dotnet", "DockerComposeManager.dll"]
