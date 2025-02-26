# Docker Compose 管理系统

这是一个基于 .NET 8 的 Docker Compose 管理系统，允许您通过 Web 界面管理多个 Docker Compose 项目。

## 功能

- 查看所有 Docker Compose 项目和服务
- 启动、停止和重启服务
- 更新服务的镜像版本
- 查看服务状态
- 高级搜索功能
- 支持管理多个目录中的 Docker Compose 项目

## 部署方式

### 使用 Docker Compose 部署

1. 克隆此仓库：

```bash
git clone https://github.com/qos-xin/docker-compose-manager.git
cd docker-compose-manager
```

2. 运行部署脚本：

```bash
chmod +x deploy.sh
./deploy.sh
```

3. 按照提示输入配置信息，可以配置多个 Docker Compose 基础目录（用逗号分隔）。

4. 部署完成后，访问 http://localhost:8080 使用系统。

### 手动配置

1. 编辑 .env 文件，设置以下环境变量：

```
# 多个目录用逗号分隔
DOCKER_COMPOSE_BASE_DIR=/path/to/docker/compose/files1,/path/to/docker/compose/files2
ADMIN_USERNAME=your-username
ADMIN_PASSWORD=your-password
JWT_SECRET=your-secret-key
```

2. 运行 Docker Compose：

```bash
docker-compose up -d
```

## 使用说明

1. 使用配置的用户名和密码登录系统。
2. 在主界面上，您可以看到所有 Docker Compose 项目和服务。
3. 使用搜索功能查找特定服务：
   - Docker Compose 项目：搜索项目名称或目录
   - 镜像名称：搜索特定的镜像
   - 版本号：搜索特定的版本
   - 状态：按服务状态筛选
4. 点击操作按钮管理服务：
   - 更新：更新服务的镜像版本
   - 重启：重启服务
   - 停止：停止服务
   - 启动：启动服务
5. 点击刷新按钮更新服务状态。

## 注意事项

- 确保 Docker 和 Docker Compose 已安装在主机上。
- 确保挂载的 Docker Compose 目录结构正确，每个项目应该有自己的目录，并包含 docker-compose.yml 文件。
- 系统需要访问 Docker 套接字 (/var/run/docker.sock) 来管理容器。
- 当配置多个基础目录时，服务将以 "基础目录名/服务目录名" 的格式显示。

## 安全建议

- 更改默认的管理员用户名和密码。
- 使用强密码和复杂的 JWT 密钥。
- 限制对管理系统的网络访问，考虑使用反向代理和 HTTPS。
