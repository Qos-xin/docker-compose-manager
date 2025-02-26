#!/bin/bash

# 确保脚本在错误时退出
set -e

echo "开始部署 Docker Compose 管理系统..."

# 检查 Docker 是否已安装
if ! command -v docker &> /dev/null; then
    echo "错误: Docker 未安装。请先安装 Docker。"
    exit 1
fi

# 检查 Docker Compose 是否已安装
if ! command -v docker-compose &> /dev/null; then
    echo "错误: Docker Compose 未安装。请先安装 Docker Compose。"
    exit 1
fi

# 检查 .env 文件是否存在，如果不存在则创建
if [ ! -f .env ]; then
    echo "创建 .env 文件..."
    
    # 提示用户输入配置信息
    docker_compose_base_dirs=""
    while true; do
        read -p "请输入 Docker Compose 文件的基础目录 (输入空行结束): " dir
        if [ -z "$dir" ]; then
            break
        fi
        
        if [ -z "$docker_compose_base_dirs" ]; then
            docker_compose_base_dirs="$dir"
        else
            docker_compose_base_dirs="$docker_compose_base_dirs,$dir"
        fi
    done
    
    if [ -z "$docker_compose_base_dirs" ]; then
        echo "错误: 至少需要一个基础目录。"
        exit 1
    fi
    
    read -p "请输入管理员用户名 [admin]: " admin_username
    admin_username=${admin_username:-admin}
    
    read -s -p "请输入管理员密码 [password]: " admin_password
    echo
    admin_password=${admin_password:-password}
    
    # 生成随机 JWT 密钥
    jwt_secret=$(openssl rand -base64 32)
    
    # 创建 .env 文件
    cat > .env << EOF
# Docker Compose 文件的基础目录（多个目录用逗号分隔）
DOCKER_COMPOSE_BASE_DIR=${docker_compose_base_dirs}

# 管理员凭据
ADMIN_USERNAME=${admin_username}
ADMIN_PASSWORD=${admin_password}

# JWT 密钥
JWT_SECRET=${jwt_secret}
EOF
    
    echo ".env 文件已创建。"
else
    echo ".env 文件已存在，使用现有配置。"
fi

# 构建并启动容器
echo "构建并启动 Docker 容器..."
docker-compose up -d --build

echo "部署完成！"
echo "您可以通过访问 http://localhost:8080 来使用 Docker Compose 管理系统。"
echo "用户名: $(grep ADMIN_USERNAME .env | cut -d= -f2)"
echo "密码: $(grep ADMIN_PASSWORD .env | cut -d= -f2)" 