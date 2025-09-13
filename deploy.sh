#!/bin/bash

set -e

# ==========================
# Config (can be overridden by env or flags)
# ==========================
APP_NAME="AccountMaintan"
APP_FILE="WebUI.dll"                # 修改为你的应用名
APP_DIR="$(pwd)"
PORT=${PORT:-7876}                       # 监听端口（可通过环境变量 PORT 覆盖）
DOTNET_VERSION="9.0"
DOTNET_SDK_INSTALL_DIR="/usr/share/dotnet"
SUPERVISOR_CONF="/etc/supervisor/conf.d/${APP_NAME}.conf"

# Build the exec command (after PORT is known)
APP_EXEC="dotnet $APP_FILE --urls http://127.0.0.1:${PORT}"

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
info "DLL 目录是 $APP_DIR"
APP_EXEC="dotnet $APP_FILE --urls http://127.0.01:${PORT}"
SUPERVISOR_CONF="/etc/supervisor/conf.d/${APP_NAME}.conf"
DOTNET_VERSION="9.0"
DOTNET_SDK_INSTALL_DIR="/usr/share/dotnet"
PORT=7051   # 监听端口

# 释放占用端口
free_port() {
  info "检查端口 $PORT 是否被占用..."
  PID=$(sudo lsof -t -i :$PORT || true)
  if [ -n "$PID" ]; then
    warn "端口 $PORT 被 PID $PID 占用，尝试终止该进程..."
    sudo kill $PID || true
    sleep 2
    if sudo lsof -t -i :$PORT >/dev/null 2>&1; then
      warn "进程未成功终止，强制杀死..."
      sudo kill -9 $PID || true
      sleep 1
    fi
    ok "端口 $PORT 已释放。"
  else
    ok "端口 $PORT 未被占用。"
  fi
}

# 检查是否安装 .NET 9.0
install_dotnet() {
  if ! dotnet --list-sdks | grep -q "^${DOTNET_VERSION}"; then
    warn ".NET SDK ${DOTNET_VERSION} 未发现，开始安装..."
    wget -q https://dotnet.microsoft.com/en-us/download/dotnet/scripts/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel "$DOTNET_VERSION" --install-dir "$DOTNET_SDK_INSTALL_DIR"
    export DOTNET_ROOT="$DOTNET_SDK_INSTALL_DIR"
    export PATH="$DOTNET_SDK_INSTALL_DIR:$PATH"
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

# 创建 supervisor 配置
setup_supervisor() {
  if [ ! -f "$APP_DIR/$APP_FILE" ]; then
    error "未找到可执行文件 $APP_FILE"
    exit 1
  fi

  info "写入 Supervisor 配置: $SUPERVISOR_CONF"
  sudo tee "$SUPERVISOR_CONF" > /dev/null <<EOF
[program:${APP_NAME}]
command=${APP_EXEC}
directory=${APP_DIR}
autostart=true
autorestart=true
stderr_logfile=/var/log/${APP_NAME}_err.log
stdout_logfile=/var/log/${APP_NAME}_out.log
environment=DOTNET_ROOT="${DOTNET_SDK_INSTALL_DIR}",ASPNETCORE_ENVIRONMENT="Production"
EOF

  sudo supervisorctl reread || true
  sudo supervisorctl update || true
  ok "Supervisor 配置已应用"

  # 释放端口，确保启动时不冲突
  free_port

  # 启动或重启服务
  if sudo supervisorctl status "${APP_NAME}" 2>/dev/null | grep -q RUNNING; then
    info "服务正在运行，执行重启..."
    sudo supervisorctl restart "${APP_NAME}"
  else
    info "启动服务..."
    sudo supervisorctl start "${APP_NAME}"
  fi
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
  ok "已启动"
}

do_stop() {
  header "停止 ${APP_NAME}"
  sudo supervisorctl stop "${APP_NAME}" || true
  ok "已停止"
}

do_restart() {
  header "重启 ${APP_NAME}"
  free_port
  sudo supervisorctl restart "${APP_NAME}" || sudo supervisorctl start "${APP_NAME}"
  ok "已重启"
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
  echo -e "${CYAN}1)${NC} 一键部署"
  echo -e "${CYAN}2)${NC} 启动"
  echo -e "${CYAN}3)${NC} 停止"
  echo -e "${CYAN}4)${NC} 重启"
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
  1                      选择 1（等价于 deploy）
  2                      选择 2（等价于 start）
  3                      选择 3（等价于 stop）
  4                      选择 4（等价于 restart）
  5                      选择 5（等价于 status）
  6                      选择 6（等价于 logs out）
  7                      选择 7（等价于 logs err）
  8                      选择 8（等价于 logs both --follow）
  9                      选择 9（等价于 释放端口）
  0                      选择 0（退出）
  deploy                 一键部署（安装依赖、写入配置、启动）
  start                  启动服务
  stop                   停止服务
  restart                重启服务
  status                 查看状态
  logs [out|err|both]    查看日志，默认 out

选项（仅 logs 有效）：
  --follow               跟随模式（tail -f）
  --lines N              显示最近 N 行，默认 200

模式切换：
  默认进入交互菜单（类似 manager.sh）
  --cli                  使用命令/数字直达模式（不进入菜单）

环境变量：
  PORT                   服务监听端口（默认 7051）
USAGE
}

# ==========================
# Entry
# ==========================
main() {
  # 默认交互菜单；除非显式传入 --cli
  if [[ "$1" != "--cli" ]]; then
    while true; do
      print_menu
    done
    exit 0
  else
    shift
  fi

  case "$1" in
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
    deploy) shift; do_deploy ;;
    start) shift; do_start ;;
    stop) shift; do_stop ;;
    restart) shift; do_restart ;;
    status) shift; do_status ;;
    logs)
      shift
      which=${1:-out}
      # parse options
      while (( "$#" )); do
        case "$1" in
          --follow) FOLLOW=1 ;;
          --lines) shift; LINES=${1:-200} ;;
        esac
        shift || true
      done
      do_logs "$which" ;;
    -h|--help) usage ;;
    *) usage; exit 1 ;;
  esac
}

main "$@"

# 兼容旧流程（直接执行脚本等价于菜单）
