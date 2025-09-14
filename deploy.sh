#!/bin/bash

set -e

# ==============================================================================
# SCRIPT CONFIGURATION
# ==============================================================================
# --- 基本应用配置 ---
APP_NAME="AccountMaintan"           # Supervisor 中的程序名
APP_FILE="WebUI.dll"                # .NET DLL 文件名

# --- 网络配置 ---
PORT=${PORT:-7876}                  # 应用监听端口 (可通过环境变量 PORT 覆盖)
# 注意: 应用将监听在 0.0.0.0 上，以接受来自 Docker 等外部服务的连接

# --- 环境配置 ---
DOTNET_VERSION="9.0"                # 需要的 .NET SDK 版本
DOTNET_SDK_INSTALL_DIR="/usr/share/dotnet" # .NET 安装目录

# ==============================================================================
# ADVANCED CONFIGURATION (usually no need to change)
# ==============================================================================
APP_DIR="$(pwd)" # App directory, detected automatically
SUPERVISOR_CONF="/etc/supervisor/conf.d/${APP_NAME}.conf" # Supervisor config file path

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

# 释放占用端口
free_port() {
  info "检查端口 $PORT 是否被占用..."
  PID=$(sudo lsof -t -i :$PORT || true)
  if [ -n "$PID" ]; then
    warn "端口 $PORT 被 PID $PID 占用，尝试终止该进程..."
    sudo kill "$PID" || true
    sleep 2
    if sudo lsof -t -i :$PORT >/dev/null 2>&1; then
      warn "进程未成功终止，强制杀死..."
      sudo kill -9 "$PID" || true
      sleep 1
    fi
    ok "端口 $PORT 已释放。"
  else
    ok "端口 $PORT 未被占用。"
  fi
}

# 检查是否安装 .NET
install_dotnet() {
  if ! dotnet --list-sdks | grep -q "^${DOTNET_VERSION}"; then
    warn ".NET SDK ${DOTNET_VERSION} 未发现，开始安装..."
    wget -q https://dotnet.microsoft.com/en-us/download/dotnet/scripts/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel "${DOTNET_VERSION}" --install-dir "${DOTNET_SDK_INSTALL_DIR}"
    export DOTNET_ROOT="${DOTNET_SDK_INSTALL_DIR}"
    export PATH="${DOTNET_SDK_INSTALL_DIR}:${PATH}"
    ok ".NET SDK ${DOTNET_VERSION} 安装完成"
  else
    ok ".NET SDK ${DOTNET_VERSION} 已安装"
  fi
}

# 检查是否安装 supervisor
install_supervisor() {
  if ! command -v supervisord >/dev/null 2>&1; then
    warn "Supervisor 未安装，开始安装..."
    sudo apt update -y
    sudo apt install -y supervisor
    ok "Supervisor 安装完成"
  else
    ok "Supervisor 已安装"
  fi
}

# 创建并应用 supervisor 配置
setup_supervisor() {
  if [ ! -f "$APP_DIR/$APP_FILE" ]; then
    error "在目录 $APP_DIR 中未找到可执行文件 $APP_FILE"
    exit 1
  fi

  # 构造启动命令，确保监听在 0.0.0.0
  local listen_urls="http://0.0.0.0:${PORT}"
  local app_exec="dotnet $APP_FILE --urls ${listen_urls}"

  info "写入 Supervisor 配置: $SUPERVISOR_CONF"
  info "启动命令将是: ${app_exec}"

  # 使用 tee 写入配置，确保权限正确
  sudo tee "$SUPERVISOR_CONF" > /dev/null <<EOF
[program:${APP_NAME}]
command=${app_exec}
directory=${APP_DIR}
autostart=true
autorestart=true
stderr_logfile=/var/log/${APP_NAME}_err.log
stdout_logfile=/var/log/${APP_NAME}_out.log
environment=DOTNET_ROOT="${DOTNET_SDK_INSTALL_DIR}",ASPNETCORE_ENVIRONMENT="Production"
EOF

  info "通知 Supervisor 重新读取所有配置..."
  sudo supervisorctl reread

  info "应用新的配置 (这将根据需要启动或重启服务)..."
  sudo supervisorctl update

  # 额外确保服务是运行状态
  info "确保服务 '${APP_NAME}' 正在运行..."
  sudo supervisorctl start "${APP_NAME}"

  ok "Supervisor 配置已成功应用"
  echo
  warn "提醒: 如果你正在使用 Nginx Proxy Manager 或类似的 Docker 反向代理,"
  warn "请确保上游主机地址设置为 Docker 宿主机 IP (通常是 172.17.0.1), 而不是 127.0.0.1."
}

# ==========================
# Actions
# ==========================
do_deploy() {
  header "一键部署 ${APP_NAME}"
  install_dotnet
  install_supervisor
  setup_supervisor
  ok "部署完成：${APP_NAME}"
}

do_start() {
  header "启动 ${APP_NAME}"
  free_port
  sudo supervisorctl start "${APP_NAME}" || true
  ok "已发送启动命令"
}

do_stop() {
  header "停止 ${APP_NAME}"
  sudo supervisorctl stop "${APP_NAME}" || true
  ok "已发送停止命令"
}

do_restart() {
  header "重启 ${APP_NAME}"
  free_port
  sudo supervisorctl restart "${APP_NAME}" || sudo supervisorctl start "${APP_NAME}"
  ok "已发送重启命令"
}

do_status() {
  header "状态 ${APP_NAME}"
  if command -v supervisorctl >/dev/null 2>&1; then
    sudo supervisorctl status "${APP_NAME}" || true
  else
    warn "supervisorctl 不可用"
  fi
  if command -v lsof >/dev/null 2>&1; then
    info "端口占用:"
    sudo lsof -i :$PORT || true
  fi
}

do_logs() {
  local which=${1:-out}  # out|err|both
  local lines=${LINES:-200}
  local follow=${FOLLOW:-}
  local out="/var/log/${APP_NAME}_out.log"
  local err="/var/log/${APP_NAME}_err.log"

  header "日志 ${APP_NAME} (${which})"
  case "$which" in
    out)
      sudo tail ${follow:+-f} -n "$lines" "$out" || true ;; 
    err)
      sudo tail ${follow:+-f} -n "$lines" "$err" || true ;; 
    both)
      warn "按 Ctrl+C 退出跟随..."
      sudo tail ${follow:+-f} -n "$lines" -v "$out" "$err" || true ;; 
    *)
      error "未知日志类型: $which (支持 out|err|both)" ;; 
  esac
}

print_menu() {
  clear
  header "${APP_NAME} 部署助手"
  echo -e "${GRAY}目录:${NC} $APP_DIR\n${GRAY}端口:${NC} $PORT\n${GRAY}.NET:${NC} $DOTNET_VERSION"
  echo
  echo -e "${CYAN}1)${NC} 一键部署 (推荐首次使用)"
  echo -e "${CYAN}2)${NC} 启动服务"
  echo -e "${CYAN}3)${NC} 停止服务"
  echo -e "${CYAN}4)${NC} 重启服务"
  echo -e "${CYAN}5)${NC} 查看状态"
  echo -e "${CYAN}6)${NC} 查看日志(输出)"
  echo -e "${CYAN}7)${NC} 查看日志(错误)"
  echo -e "${CYAN}8)${NC} 跟随日志(输出+错误)"
  echo -e "${CYAN}9)${NC} 释放端口($PORT)"
  echo -e "${CYAN}0)${NC} 退出"
  echo
  read -rp "请选择操作: " choice
  case "$choice" in
    1) do_deploy ;; 
    2) do_start ;; 
    3) do_stop ;; 
    4) do_restart ;; 
    5) do_status ;; 
    6) do_logs out ;; 
    7) do_logs err ;; 
    8) FOLLOW=1 do_logs both ;; 
    9) free_port ;; 
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
  (无)                    进入交互菜单
  deploy                 一键部署（安装依赖、写入配置、启动）
  start                  启动服务
  stop                   停止服务
  restart                重启服务
  status                 查看状态
  logs [out|err|both]    查看日志 (默认: out)

选项 (仅 logs 有效):
  --follow               跟随模式 (tail -f)
  --lines N              显示最近 N 行 (默认: 200)

环境变量:
  PORT                   服务监听端口 (默认: ${PORT})
USAGE
}

# ==========================
# Entry
# ==========================
main() {
  # 如果没有参数，则显示交互菜单
  if [ -z "$1" ]; then
    while true; do
      print_menu
    done
    exit 0
  fi

  # 否则，处理命令行参数
  case "$1" in
    deploy|start|stop|restart|status)
      "do_$1"
      ;;
    logs)
      shift
      local which="out"
      # 简单地处理一下参数，更复杂的可以用 getopts
      if [[ "$1" != "--"* && -n "$1" ]]; then
        which="$1"
        shift
      fi
      FOLLOW=
      LINES=200
      if [[ "$*" == *"--follow"* ]]; then
        FOLLOW=1
      fi
      # 注意: 简单的行数解析
      # for a robust solution, use getopts
      
      do_logs "$which"
      ;;
    -h|--help)
      usage
      ;;
    *)
      usage
      exit 1
      ;;
  esac
}

main "$@"
