#!/bin/bash

set -e

# ==========================
# Config (can be overridden by env or flags)
# ==========================
APP_NAME="nginx-proxy-manager"
CONTAINER_NAME="npm"
IMAGE_NAME="docker.io/jc21/nginx-proxy-manager:latest"
DATA_DIR="./data"
LETSENCRYPT_DIR="./letsencrypt"
HTTP_PORT=${HTTP_PORT:-80}
ADMIN_PORT=${ADMIN_PORT:-81}
HTTPS_PORT=${HTTPS_PORT:-443}

# ==========================
# UI Helpers
# ==========================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

info()  { echo -e "${CYAN}➤${NC} $*"; }
ok()    { echo -e "${GREEN}✓${NC} $*"; }
warn()  { echo -e "${YELLOW}!${NC} $*"; }
error() { echo -e "${RED}✗${NC} $*"; }

header() {
  echo -e "${CYAN}============================================${NC}"
  echo -e "${CYAN}$*${NC}"
  echo -e "${CYAN}============================================${NC}"
}

# 获取 Docker 宿主机 IP
get_docker_host_ip() {
  local ip
  if command -v ip >/dev/null 2>&1 && ip addr show docker0 >/dev/null 2>&1; then
    ip=$(ip addr show docker0 | grep -Po 'inet \K[\d.]+')
  fi
  # 如果找不到，则返回默认值
  echo "${ip:-172.17.0.1}"
}

# 检查Docker是否安装
check_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    warn "Docker 未安装，开始自动安装..."
    install_docker
  else
    ok "Docker 已安装"
  fi
}

# 清理所有Docker配置
cleanup_docker_configs() {
  info "清理所有旧的Docker配置..."
  sudo rm -f /etc/apt/sources.list.d/docker*.list
  sudo rm -f /etc/apt/keyrings/docker*.gpg
  sudo rm -f /etc/apt/sources.list.d/*docker*
  sudo rm -f /etc/apt/keyrings/*docker*
  ok "旧配置清理完成"
}

# 自动安装Docker
install_docker() {
  info "检测系统类型..."
  
  # 首先清理所有旧配置
  cleanup_docker_configs
  
  # 检测操作系统
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$NAME
    VER=$VERSION_ID
    CODENAME=$VERSION_CODENAME
  elif type lsb_release >/dev/null 2>&1; then
    OS=$(lsb_release -si)
    VER=$(lsb_release -sr)
    CODENAME=$(lsb_release -cs)
  elif [ -f /etc/lsb-release ]; then
    . /etc/lsb-release
    OS=$DISTRIB_ID
    VER=$DISTRIB_RELEASE
    CODENAME=$DISTRIB_CODENAME
  elif [ -f /etc/debian_version ]; then
    OS=Debian
    VER=$(cat /etc/debian_version)
    CODENAME="unknown"
  else
    OS=$(uname -s)
    VER=$(uname -r)
    CODENAME="unknown"
  fi
  
  info "检测到系统: $OS $VER (代号: $CODENAME)"
  
  # 根据系统类型安装Docker
  case "$OS" in
    *"Ubuntu"*) 
      install_docker_ubuntu_debian
      ;;
    *"Debian"*) 
      # 检查是否是 Debian Trixie 或其他开发版
      if [[ "$CODENAME" == "trixie" ]] || [[ "$CODENAME" == "sid" ]] || [[ "$CODENAME" == "unstable" ]] || [[ "$VER" =~ ^1[3-9]$ ]] || [[ "$VER" =~ ^[2-9][0-9]$ ]]; then
        warn "检测到 Debian 开发版 ($CODENAME $VER)，使用特殊处理"
        install_docker_debian_trixie
      else
        install_docker_ubuntu_debian
      fi
      ;;
    *"CentOS"*|*"Red Hat"*|*"Rocky"*|*"AlmaLinux"*) 
      install_docker_centos_rhel
      ;;
    *"Fedora"*) 
      install_docker_fedora
      ;;
    *"Arch"*) 
      install_docker_arch
      ;;
    *"macOS"*|*"Darwin"*) 
      install_docker_macos
      ;;
    *)
      error "不支持的操作系统: $OS"
      error "请手动安装 Docker: https://docs.docker.com/get-docker/"
      exit 1
      ;;
  esac
}

# Ubuntu/Debian 安装Docker
install_docker_ubuntu_debian() {
  info "在 Ubuntu/Debian 上安装 Docker..."
  
  # 更新包索引
  info "更新包索引..."
  sudo apt-get update -y
  
  # 安装必要的包
  info "安装必要的包..."
  sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release
  
  # 检测发行版和版本
  local distro=$(lsb_release -is)
  local codename=$(lsb_release -cs)
  local version_id=""
  
  # 从 /etc/os-release 获取版本ID
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    version_id="$VERSION_ID"
  fi
  
  info "检测到发行版: $distro $codename (版本: $version_id)"
  
  # 处理不支持的发行版版本
  case "$codename" in
    "trixie"|"sid"|"unstable")
      warn "检测到开发版/不稳定版 $codename，使用 bookworm 仓库"
      codename="bookworm"
      distro="debian"
      ;;
    "noble"|"jammy"|"focal"|"bionic")
      # Ubuntu 版本，使用 Ubuntu 仓库
      distro="ubuntu"
      ;;
    "bookworm"|"bullseye"|"buster")
      # Debian 版本，使用 Debian 仓库
      distro="debian"
      ;;
    *)
      # 检查版本号，如果是13或更高，可能是trixie
      if [[ "$version_id" =~ ^1[3-9]$ ]] || [[ "$version_id" =~ ^[2-9][0-9]$ ]]; then
        warn "检测到高版本号 $version_id，可能是开发版，使用 bookworm 仓库"
        codename="bookworm"
        distro="debian"
      else
        warn "未知的发行版版本 $codename，尝试使用 bookworm"
        codename="bookworm"
        distro="debian"
      fi
      ;;
  esac
  
  # 添加Docker官方GPG密钥
  info "添加Docker官方GPG密钥..."
  sudo mkdir -p /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/$distro/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  
  # 设置稳定版仓库
  info "设置Docker仓库 ($distro $codename)..."
  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/$distro \ 
    $codename stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
  
  # 更新包索引
  info "更新包索引..."
  sudo apt-get update -y
  
  # 检查仓库是否可用
  if ! apt-cache policy docker-ce >/dev/null 2>&1; then
    warn "Docker 仓库不可用，尝试使用系统包管理器安装..."
    install_docker_from_system_repo
    return
  fi
  
  # 安装Docker Engine
  info "安装Docker Engine..."
  sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  
  # 启动Docker服务
  info "启动Docker服务..."
  sudo systemctl start docker
  sudo systemctl enable docker
  
  # 将当前用户添加到docker组
  info "将当前用户添加到docker组..."
  sudo usermod -aG docker $USER
  
  ok "Docker 安装完成"
  warn "请重新登录或运行 'newgrp docker' 以使组权限生效"
}

# Debian Trixie 专用安装函数
install_docker_debian_trixie() {
  info "在 Debian Trixie 上安装 Docker..."
  
  # 首先清理所有旧的Docker配置
  info "清理旧的Docker配置..."
  sudo rm -f /etc/apt/sources.list.d/docker.list
  sudo rm -f /etc/apt/sources.list.d/docker-ce.list
  sudo rm -f /etc/apt/keyrings/docker.gpg
  sudo rm -f /etc/apt/keyrings/docker-ce.gpg
  
  # 更新包索引（清理后）
  info "更新包索引..."
  sudo apt-get update -y
  
  # 安装必要的包
  info "安装必要的包..."
  sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release
  
  # 添加Docker官方GPG密钥
  info "添加Docker官方GPG密钥..."
  sudo mkdir -p /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  
  # 设置 Debian Bookworm 仓库（Trixie 使用 Bookworm 的包）
  info "设置 Debian Bookworm 仓库..."
  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian \ 
    bookworm stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
  
  # 更新包索引
  info "更新包索引..."
  sudo apt-get update -y
  
  # 检查仓库是否可用
  if ! apt-cache policy docker-ce >/dev/null 2>&1; then
    warn "Docker 官方仓库不可用，使用系统仓库安装..."
    install_docker_from_system_repo
    return
  fi
  
  # 安装Docker Engine
  info "安装Docker Engine..."
  sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  
  # 启动Docker服务
  info "启动Docker服务..."
  sudo systemctl start docker
  sudo systemctl enable docker
  
  # 将当前用户添加到docker组
  info "将当前用户添加到docker组..."
  sudo usermod -aG docker $USER
  
  ok "Docker 安装完成（Debian Trixie 专用）"
  warn "请重新登录或运行 'newgrp docker' 以使组权限生效"
}

# 从系统仓库安装Docker（备用方案）
install_docker_from_system_repo() {
  info "从系统仓库安装 Docker..."
  
  # 清理所有Docker相关配置
  info "清理所有Docker配置..."
  sudo rm -f /etc/apt/sources.list.d/docker.list
  sudo rm -f /etc/apt/sources.list.d/docker-ce.list
  sudo rm -f /etc/apt/keyrings/docker.gpg
  sudo rm -f /etc/apt/keyrings/docker-ce.gpg
  
  # 更新包索引
  sudo apt-get update -y
  
  # 安装Docker
  sudo apt-get install -y docker.io docker-compose
  
  # 启动Docker服务
  info "启动Docker服务..."
  sudo systemctl start docker
  sudo systemctl enable docker
  
  # 将当前用户添加到docker组
  info "将当前用户添加到docker组..."
  sudo usermod -aG docker $USER
  
  ok "Docker 安装完成（使用系统仓库）"
  warn "请重新登录或运行 'newgrp docker' 以使组权限生效"
}

# CentOS/RHEL/Rocky/AlmaLinux 安装Docker
install_docker_centos_rhel() {
  info "在 CentOS/RHEL 上安装 Docker..."
  
  # 安装必要的包
  info "安装必要的包..."
  sudo yum install -y yum-utils
  
  # 添加Docker仓库
  info "添加Docker仓库..."
  sudo yum-config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
  
  # 安装Docker Engine
  info "安装Docker Engine..."
  sudo yum install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  
  # 启动Docker服务
  info "启动Docker服务..."
  sudo systemctl start docker
  sudo systemctl enable docker
  
  # 将当前用户添加到docker组
  info "将当前用户添加到docker组..."
  sudo usermod -aG docker $USER
  
  ok "Docker 安装完成"
  warn "请重新登录或运行 'newgrp docker' 以使组权限生效"
}

# Fedora 安装Docker
install_docker_fedora() {
  info "在 Fedora 上安装 Docker..."
  
  # 安装必要的包
  info "安装必要的包..."
  sudo dnf install -y dnf-plugins-core
  
  # 添加Docker仓库
  info "添加Docker仓库..."
  sudo dnf config-manager --add-repo https://download.docker.com/linux/fedora/docker-ce.repo
  
  # 安装Docker Engine
  info "安装Docker Engine..."
  sudo dnf install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  
  # 启动Docker服务
  info "启动Docker服务..."
  sudo systemctl start docker
  sudo systemctl enable docker
  
  # 将当前用户添加到docker组
  info "将当前用户添加到docker组..."
  sudo usermod -aG docker $USER
  
  ok "Docker 安装完成"
  warn "请重新登录或运行 'newgrp docker' 以使组权限生效"
}

# Arch Linux 安装Docker
install_docker_arch() {
  info "在 Arch Linux 上安装 Docker..."
  
  # 安装Docker
  info "安装Docker..."
  sudo pacman -S --noconfirm docker docker-compose
  
  # 启动Docker服务
  info "启动Docker服务..."
  sudo systemctl start docker
  sudo systemctl enable docker
  
  # 将当前用户添加到docker组
  info "将当前用户添加到docker组..."
  sudo usermod -aG docker $USER
  
  ok "Docker 安装完成"
  warn "请重新登录或运行 'newgrp docker' 以使组权限生效"
}

# macOS 安装Docker
install_docker_macos() {
  info "在 macOS 上安装 Docker..."
  
  # 检查是否安装了Homebrew
  if ! command -v brew >/dev/null 2>&1; then
    error "请先安装 Homebrew: https://brew.sh/"
    exit 1
  fi
  
  # 安装Docker Desktop
  info "通过Homebrew安装Docker Desktop..."
  brew install --cask docker
  
  # 启动Docker Desktop
  info "启动Docker Desktop..."
  open /Applications/Docker.app
  
  # 等待Docker启动
  info "等待Docker启动..."
  local max_attempts=30
  local attempt=1
  
  while [ $attempt -le $max_attempts ]; do
    if docker info >/dev/null 2>&1; then
      ok "Docker 启动成功"
      return 0
    fi
    info "等待Docker启动... ($attempt/$max_attempts)"
    sleep 5
    ((attempt++))
  done
  
  warn "Docker 启动超时，请手动启动 Docker Desktop"
}

# 检查Docker Compose是否安装
check_docker_compose() {
  if ! command -v docker-compose >/dev/null 2>&1 && ! docker compose version >/dev/null 2>&1; then
    warn "Docker Compose 未安装，开始自动安装..."
    install_docker_compose
  else
    ok "Docker Compose 已安装"
  fi
}

# 自动安装Docker Compose
install_docker_compose() {
  info "安装 Docker Compose..."
  
  # 检测操作系统
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$NAME
  elif type lsb_release >/dev/null 2>&1; then
    OS=$(lsb_release -si)
  else
    OS=$(uname -s)
  fi
  
  case "$OS" in
    *"Ubuntu"*|*"Debian"*|*"CentOS"*|*"Red Hat"*|*"Rocky"*|*"AlmaLinux"*|*"Fedora"*) 
      # 对于这些系统，Docker Compose 通常已经包含在 Docker 安装中
      # 如果没有，尝试安装 docker-compose-plugin
      if command -v apt-get >/dev/null 2>&1; then
        sudo apt-get install -y docker-compose-plugin
      elif command -v yum >/dev/null 2>&1; then
        sudo yum install -y docker-compose-plugin
      elif command -v dnf >/dev/null 2>&1; then
        sudo dnf install -y docker-compose-plugin
      fi
      ;;
    *"Arch"*) 
      sudo pacman -S --noconfirm docker-compose
      ;;
    *"macOS"*|*"Darwin"*) 
      # macOS 通过 Homebrew 安装
      if command -v brew >/dev/null 2>&1; then
        brew install docker-compose
      else
        error "请先安装 Homebrew: https://brew.sh/"
        exit 1
      fi
      ;;
    *)
      # 其他系统，下载二进制文件
      info "下载 Docker Compose 二进制文件..."
      local compose_version="v2.24.0"
      local arch=$(uname -m)
      case $arch in
        x86_64) arch="x86_64" ;; 
        aarch64|arm64) arch="aarch64" ;; 
        armv7l) arch="armv7" ;; 
        *) error "不支持的架构: $arch"; exit 1 ;; 
      esac
      
      sudo curl -L "https://github.com/docker/compose/releases/download/${compose_version}/docker-compose-$(uname -s)-${arch}" -o /usr/local/bin/docker-compose
      sudo chmod +x /usr/local/bin/docker-compose
      ;;
  esac
  
  # 验证安装
  if command -v docker-compose >/dev/null 2>&1 || docker compose version >/dev/null 2>&1; then
    ok "Docker Compose 安装完成"
  else
    error "Docker Compose 安装失败"
    exit 1
  fi
}

# 检查端口是否被占用
check_ports() {
  local ports=($HTTP_PORT $ADMIN_PORT $HTTPS_PORT)
  for port in "${ports[@]}"; do
    if lsof -t -i :$port >/dev/null 2>&1; then
      warn "端口 $port 被占用，尝试释放..."
      PID=$(lsof -t -i :$port)
      kill $PID 2>/dev/null || true
      sleep 2
      if lsof -t -i :$port >/dev/null 2>&1; then
        warn "强制杀死进程 $PID..."
        kill -9 $PID 2>/dev/null || true
        sleep 1
      fi
      ok "端口 $port 已释放"
    else
      ok "端口 $port 可用"
    fi
  done
}

# 创建必要的目录
create_directories() {
  info "创建数据目录..."
  mkdir -p "$DATA_DIR"
  mkdir -p "$LETSENCRYPT_DIR"
  ok "目录创建完成"
}

# 创建docker-compose.yml文件
create_docker_compose() {
  info "创建 docker-compose.yml 文件..."
  cat > docker-compose.yml <<EOF
version: '3.8'

services:
  app:
    image: '${IMAGE_NAME}'
    container_name: ${CONTAINER_NAME}
    restart: unless-stopped
    ports:
      - '${HTTP_PORT}:80'
      - '${ADMIN_PORT}:81'
      - '${HTTPS_PORT}:443'
    volumes:
      - ${DATA_DIR}:/data
      - ${LETSENCRYPT_DIR}:/etc/letsencrypt
    environment:
      - DB_SQLITE_FILE=/data/database.sqlite
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:81"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
EOF
  ok "docker-compose.yml 创建完成"
}

# 拉取镜像
pull_image() {
  info "拉取 Nginx Proxy Manager 镜像..."
  docker pull "$IMAGE_NAME"
  ok "镜像拉取完成"
}

# 启动服务
start_services() {
  info "启动 Nginx Proxy Manager..."
  
  # 检查是否使用新版本的docker compose
  if docker compose version >/dev/null 2>&1; then
    docker compose up -d
  else
    docker-compose up -d
  fi
  
  ok "服务启动完成"
}

# 等待服务就绪
wait_for_service() {
  info "等待服务启动..."
  local max_attempts=30
  local attempt=1
  
  while [ $attempt -le $max_attempts ]; do
    if curl -s http://localhost:$ADMIN_PORT >/dev/null 2>&1; then
      ok "服务已就绪"
      return 0
    fi
    info "等待中... ($attempt/$max_attempts)"
    sleep 2
    ((attempt++))
  done
  
  warn "服务启动超时，请检查日志"
  return 1
}

# 显示通用访问信息
display_info() {
  local docker_host_ip
  docker_host_ip=$(get_docker_host_ip)

  header "NPM 访问信息和提示"
  echo -e "${CYAN}访问信息：${NC}"
  echo -e "  管理界面: ${YELLOW}http://<你的服务器IP>:$ADMIN_PORT${NC}"
  echo
  echo -e "${CYAN}默认登录信息：${NC}"
  echo -e "  邮箱: ${YELLOW}admin@example.com${NC}"
  echo -e "  密码: ${YELLOW}changeme${NC}"
  echo
  echo -e "${YELLOW}⚠️  如果尚未登录，请立即登录并修改默认密码！${NC}"
  echo
  echo -e "${CYAN}数据目录：${NC}"
  echo -e "  应用数据: ${GRAY}$(pwd)/$DATA_DIR${NC}"
  echo -e "  SSL证书: ${GRAY}$(pwd)/$LETSENCRYPT_DIR${NC}"
  echo
  echo -e "${CYAN}--- 反向代理重要提示 ---${NC}"
  echo -e "  当你在 NPM 中添加代理主机以指向"
  echo -e "  ${YELLOW}运行在此服务器上的其他服务${NC} (非Docker服务) 时,"
  echo -e "  请使用 Docker 宿主机 IP: ${GREEN}${docker_host_ip}${NC}, app是npm自己"
  echo -e "  例如: ${GRAY}http://${docker_host_ip}:<你的应用端口>${NC}"
}

# 显示安装完成后的信息
show_access_info_after_install() {
  header "安装完成！"
  echo -e "${GREEN}Nginx Proxy Manager 已成功安装并启动${NC}"
  echo
  display_info
}

# ==========================
# Actions
# ==========================
do_install() {
  header "安装 Nginx Proxy Manager"
  check_docker
  check_docker_compose
  check_ports
  create_directories
  create_docker_compose
  pull_image
  start_services
  wait_for_service
  show_access_info_after_install
}

do_start() {
  header "启动 Nginx Proxy Manager"
  check_ports
  if [ -f "docker-compose.yml" ]; then
    if docker compose version >/dev/null 2>&1; then
      docker compose up -d
    else
      docker-compose up -d
    fi
    ok "服务已启动"
  else
    error "未找到 docker-compose.yml 文件"
    exit 1
  fi
}

do_stop() {
  header "停止 Nginx Proxy Manager"
  if [ -f "docker-compose.yml" ]; then
    if docker compose version >/dev/null 2>&1; then
      docker compose down
    else
      docker-compose down
    fi
    ok "服务已停止"
  else
    error "未找到 docker-compose.yml 文件"
    exit 1
  fi
}

do_restart() {
  header "重启 Nginx Proxy Manager"
  do_stop
  sleep 2
  do_start
}

do_status() {
  header "Nginx Proxy Manager 状态"
  if [ -f "docker-compose.yml" ]; then
    if docker compose version >/dev/null 2>&1; then
      docker compose ps
    else
      docker-compose ps
    fi
  else
    warn "未找到 docker-compose.yml 文件"
  fi
  
  echo
  info "端口占用情况:"
  lsof -i :$HTTP_PORT -i :$ADMIN_PORT -i :$HTTPS_PORT 2>/dev/null || true
}

do_logs() {
  local follow=${1:-false}
  header "Nginx Proxy Manager 日志"
  if [ -f "docker-compose.yml" ]; then
    if [ "$follow" = "true" ]; then
      if docker compose version >/dev/null 2>&1; then
        docker compose logs -f
      else
        docker-compose logs -f
      fi
    else
      if docker compose version >/dev/null 2>&1; then
        docker compose logs --tail=50
      else
        docker-compose logs --tail=50
      fi
    fi
  else
    error "未找到 docker-compose.yml 文件"
    exit 1
  fi
}

do_update() {
  header "更新 Nginx Proxy Manager"
  info "拉取最新镜像..."
  docker pull "$IMAGE_NAME"
  do_restart
  ok "更新完成"
}

do_remove() {
  header "卸载 Nginx Proxy Manager"
  warn "这将删除容器和镜像，但保留数据目录"
  read -p "确定要继续吗？(y/N): " confirm
  if [[ $confirm =~ ^[Yy]$ ]]; then
    do_stop
    docker rmi "$IMAGE_NAME" 2>/dev/null || true
    ok "卸载完成"
    echo -e "${YELLOW}数据目录已保留: $DATA_DIR, $LETSENCRYPT_DIR${NC}"
  else
    info "操作已取消"
  fi
}

do_help() {
  display_info
}

do_edit_config() {
  header "编辑 docker-compose.yml"
  local config_file="docker-compose.yml"

  if [ ! -f "$config_file" ]; then
    error "配置文件 $config_file 不存在。请先执行安装。"
    return 1
  fi

  # 查找合适的编辑器
  local editor
  if [ -n "$EDITOR" ]; then
    editor="$EDITOR"
  elif command -v nano >/dev/null 2>&1; then
    editor="nano"
  elif command -v vim >/dev/null 2>&1; then
    editor="vim"
  elif command -v vi >/dev/null 2>&1; then
    editor="vi"
  else
    error "未找到可用的文本编辑器 (nano, vim, vi)。请手动编辑 $config_file。"
    return 1
  fi

  info "使用 $editor 打开 $config_file ..."
  $editor "$config_file"

  echo
  read -p "编辑完成。是否要重启 NPM 以应用更改？(y/N): " confirm
  if [[ $confirm =~ ^[Yy]$ ]]; then
    do_restart
  else
    info "操作已取消。配置更改将在下次重启时生效。"
  fi
}

print_menu() {
  clear
  header "Nginx Proxy Manager 管理工具"
  echo -e "${GRAY}HTTP端口:${NC} $HTTP_PORT"
  echo -e "${GRAY}管理端口:${NC} $ADMIN_PORT" 
  echo -e "${GRAY}HTTPS端口:${NC} $HTTPS_PORT"
  echo -e "${GRAY}数据目录:${NC} $(pwd)/$DATA_DIR"
  echo
  echo -e "${CYAN}1)${NC} 安装 NPM"
  echo -e "${CYAN}2)${NC} 启动"
  echo -e "${CYAN}3)${NC} 停止"
  echo -e "${CYAN}4)${NC} 重启"
  echo -e "${CYAN}5)${NC} 查看状态"
  echo -e "${CYAN}6)${NC} 查看日志"
  echo -e "${CYAN}7)${NC} 跟随日志"
  echo -e "${CYAN}8)${NC} 更新"
  echo -e "${CYAN}9)${NC} 卸载"
  echo -e "${CYAN}10)${NC} 帮助/显示信息"
  echo -e "${CYAN}11)${NC} 编辑配置并重启"
  echo -e "${CYAN}0)${NC} 退出"
  echo
  read -rp "请选择操作: " choice
  case "$choice" in
    1) do_install ;; 
    2) do_start ;; 
    3) do_stop ;; 
    4) do_restart ;; 
    5) do_status ;; 
    6) do_logs false ;; 
    7) do_logs true ;; 
    8) do_update ;; 
    9) do_remove ;; 
    10) do_help ;; 
    11) do_edit_config ;; 
    0) exit 0 ;; 
    *) warn "无效选择" ;; 
  esac
  echo
  read -rp "按回车键返回菜单..." _
}

usage() {
  cat <<USAGE
用法: $0 [命令] [选项] 

命令:
  (无)                   进入交互菜单
  install                安装 NPM
  start                  启动服务
  stop                   停止服务
  restart                重启服务
  status                 查看状态
  logs [--follow]        查看日志
  update                 更新到最新版本
  remove                 卸载 NPM
  help                   显示访问信息和帮助
  edit                   编辑 docker-compose.yml 并重启

选项：
  --follow               跟随模式查看日志

环境变量：
  HTTP_PORT              HTTP端口（默认 80）
  ADMIN_PORT             管理端口（默认 81）
  HTTPS_PORT             HTTPS端口（默认 443）
USAGE
}

# ==========================
# Entry
# ==========================
main() {
  # 默认交互菜单
  if [ -z "$1" ]; then
    while true; do
      print_menu
    done
    exit 0
  fi

  case "$1" in
    install) shift; do_install ;; 
    start) shift; do_start ;; 
    stop) shift; do_stop ;; 
    restart) shift; do_restart ;; 
    status) shift; do_status ;; 
    logs) 
      shift
      follow=false
      if [[ "$1" == "--follow" ]]; then
        follow=true
      fi
      do_logs "$follow" ;; 
    update) shift; do_update ;; 
    remove) shift; do_remove ;; 
    help) shift; do_help ;; 
    edit) shift; do_edit_config ;; 
    -h|--help) usage ;; 
    *) usage; exit 1 ;; 
  esac
}

main "$@"
