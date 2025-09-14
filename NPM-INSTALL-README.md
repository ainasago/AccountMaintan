# Nginx Proxy Manager 一键安装脚本

这是一个参考 `deploy.sh` 脚本风格编写的 Nginx Proxy Manager 一键安装脚本。

## 功能特性

- 🚀 一键安装 Nginx Proxy Manager
- 🐳 **自动安装 Docker 和 Docker Compose**（支持多种Linux发行版和macOS）
- 📦 自动创建 docker-compose.yml 配置
- 🔧 支持端口自定义配置
- 📊 提供完整的管理功能（启动、停止、重启、日志查看等）
- 🎨 美观的彩色输出界面
- 📋 交互式菜单和命令行模式
- 🔍 智能系统检测和依赖管理

## 系统要求

- Linux/macOS 系统
- 网络连接（用于下载Docker和镜像）
- sudo权限（用于安装Docker）

**注意**: 脚本会自动检测并安装Docker和Docker Compose，无需预先安装！

## 支持的操作系统

### Linux 发行版
- **Ubuntu** (18.04+)
- **Debian** (9+)
  - 包括 Debian Trixie (开发版)
  - 自动回退到稳定版仓库
- **CentOS** (7+)
- **Red Hat Enterprise Linux** (7+)
- **Rocky Linux** (8+)
- **AlmaLinux** (8+)
- **Fedora** (30+)
- **Arch Linux**

### macOS
- **macOS** (需要先安装 Homebrew)

### 其他系统
- 对于其他Linux发行版，脚本会尝试下载Docker Compose二进制文件

## 自动安装过程

脚本会自动执行以下步骤：

1. **系统检测** - 自动识别操作系统类型和版本
2. **Docker安装** - 根据系统类型安装Docker Engine
3. **Docker Compose安装** - 安装Docker Compose或插件
4. **权限配置** - 将当前用户添加到docker组
5. **服务启动** - 启动并启用Docker服务
6. **NPM安装** - 拉取镜像并启动Nginx Proxy Manager

## 使用方法

### 1. 下载脚本

```bash
# 下载脚本
wget https://raw.githubusercontent.com/your-repo/install-npm.sh
# 或者直接复制脚本内容到本地文件

# 添加执行权限
chmod +x install-npm.sh
```

### 2. 运行脚本

#### 交互式菜单模式（推荐）
```bash
./install-npm.sh
```

#### 命令行模式
```bash
# 安装
./install-npm.sh --cli install

# 启动
./install-npm.sh --cli start

# 停止
./install-npm.sh --cli stop

# 重启
./install-npm.sh --cli restart

# 查看状态
./install-npm.sh --cli status

# 查看日志
./install-npm.sh --cli logs

# 跟随日志
./install-npm.sh --cli logs --follow

# 更新
./install-npm.sh --cli update

# 卸载
./install-npm.sh --cli remove
```

## 环境变量配置

可以通过环境变量自定义端口：

```bash
# 自定义端口
export HTTP_PORT=8080
export ADMIN_PORT=8081
export HTTPS_PORT=8443

# 然后运行安装
./install-npm.sh --cli install
```

## 默认配置

- **HTTP端口**: 80
- **管理端口**: 81
- **HTTPS端口**: 443
- **数据目录**: `./data`
- **SSL证书目录**: `./letsencrypt`

## 访问信息

安装完成后：

- **管理界面**: http://localhost:81
- **默认邮箱**: admin@example.com
- **默认密码**: changeme

⚠️ **重要**: 请立即登录并修改默认密码！

## 目录结构

```
.
├── install-npm.sh          # 安装脚本
├── docker-compose.yml      # 自动生成的配置文件
├── data/                   # 应用数据目录
└── letsencrypt/           # SSL证书目录
```

## 功能说明

### 1. 安装 (install)
- 检查 Docker 和 Docker Compose
- 检查端口占用并自动释放
- 创建必要的目录
- 生成 docker-compose.yml 配置
- 拉取最新镜像
- 启动服务

### 2. 管理功能
- **启动**: 启动 NPM 服务
- **停止**: 停止 NPM 服务
- **重启**: 重启 NPM 服务
- **状态**: 查看服务运行状态和端口占用
- **日志**: 查看服务日志
- **更新**: 拉取最新镜像并重启
- **卸载**: 删除容器和镜像（保留数据）

### 3. 端口管理
脚本会自动检查端口占用情况，如果发现端口被占用会尝试释放：
- 先尝试正常终止进程
- 如果失败则强制杀死进程
- 确保服务能够正常启动

## 故障排除

### 1. Docker 安装问题

#### 权限问题
```bash
# 如果遇到权限问题，确保用户有sudo权限
sudo usermod -aG sudo $USER
# 重新登录后重试
```

#### Docker 服务未启动
```bash
# 手动启动Docker服务
sudo systemctl start docker
sudo systemctl enable docker

# 检查Docker状态
sudo systemctl status docker
```

#### 用户组权限问题
```bash
# 将用户添加到docker组
sudo usermod -aG docker $USER

# 应用组权限（无需重新登录）
newgrp docker

# 验证权限
docker run hello-world
```

### 2. 端口被占用
```bash
# 手动检查端口占用
lsof -i :80
lsof -i :81
lsof -i :443

# 手动释放端口
sudo kill -9 $(lsof -t -i :80)
```

### 3. 网络问题
```bash
# 检查网络连接
ping google.com

# 检查Docker Hub连接
docker pull hello-world

# 如果使用代理，配置Docker代理
sudo mkdir -p /etc/systemd/system/docker.service.d
sudo tee /etc/systemd/system/docker.service.d/http-proxy.conf > /dev/null <<EOF
[Service]
Environment="HTTP_PROXY=http://proxy.example.com:8080"
Environment="HTTPS_PROXY=http://proxy.example.com:8080"
Environment="NO_PROXY=localhost,127.0.0.1"
EOF
sudo systemctl daemon-reload
sudo systemctl restart docker
```

### 4. 系统特定问题

#### Ubuntu/Debian
```bash
# 如果遇到GPG密钥问题
sudo apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 7EA0A9C3F273FCD8

# 更新包列表
sudo apt-get update

# 如果遇到 Debian Trixie 仓库问题
# 脚本会自动回退到 bookworm 仓库
# 或者手动使用系统仓库安装
sudo apt-get install docker.io docker-compose
```

#### Debian Trixie 特殊处理
对于 Debian Trixie (开发版)，脚本会：
1. 自动检测到开发版
2. 回退使用 Debian Bookworm 的稳定仓库
3. 如果官方仓库不可用，自动使用系统仓库安装

#### CentOS/RHEL
```bash
# 如果遇到仓库问题
sudo yum-config-manager --disable docker-ce-stable
sudo yum-config-manager --enable docker-ce-stable
```

#### macOS
```bash
# 如果Homebrew安装失败
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# 如果Docker Desktop启动失败
open /Applications/Docker.app
```

### 5. 查看详细日志
```bash
# 跟随日志输出
./install-npm.sh --cli logs --follow

# 或者直接使用 docker compose
docker compose logs -f

# 查看Docker系统日志
sudo journalctl -u docker.service
```

## 高级配置

### 自定义 docker-compose.yml

脚本会自动生成 `docker-compose.yml` 文件，你也可以手动修改：

```yaml
version: '3.8'

services:
  app:
    image: 'docker.io/jc21/nginx-proxy-manager:latest'
    container_name: npm
    restart: unless-stopped
    ports:
      - '80:80'
      - '81:81'
      - '443:443'
    volumes:
      - ./data:/data
      - ./letsencrypt:/etc/letsencrypt
    environment:
      - DB_SQLITE_FILE=/data/database.sqlite
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:81"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
```

## 参考

- [Nginx Proxy Manager 官方仓库](https://github.com/NginxProxyManager/nginx-proxy-manager)
- [Nginx Proxy Manager 文档](https://nginxproxymanager.com)
- [Docker 安装指南](https://docs.docker.com/get-docker/)
- [Docker Compose 安装指南](https://docs.docker.com/compose/install/)

## 许可证

MIT License
