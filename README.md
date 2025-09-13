# 账号资源管理系统 (Account Maintenance System)

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-green)
![SQLite](https://img.shields.io/badge/SQLite-3-lightblue)
![FreeSql](https://img.shields.io/badge/FreeSql-3.5.207-orange)
![Hangfire](https://img.shields.io/badge/Hangfire-1.8.21-red)
![License](https://img.shields.io/badge/License-MIT-yellow)

一个功能完整的账号密码管理系统，支持安全存储、智能提醒、多通道通知、数据导入导出等功能

[功能特性](#功能特性) • [快速开始](#快速开始) • [使用说明](#使用说明) • [技术栈](#技术栈) • [项目结构](#项目结构) • [部署指南](#部署指南)

</div>

## 📋 项目简介

账号资源管理系统是一个基于 ASP.NET Core 9.0 开发的现代化账号密码管理平台，旨在帮助用户安全、高效地管理各种在线账号。系统采用现代化的技术栈，提供完整的用户认证、数据加密、智能提醒、多通道通知等功能。

### 🎯 核心价值

- **安全第一**：采用AES加密存储敏感数据，支持TOTP双因素认证
- **智能提醒**：基于Hangfire的定时任务，自动检测长期未访问的账号
- **多通道通知**：支持邮件、Telegram、SignalR实时通知
- **数据管理**：支持CSV/Excel导入导出，数据备份恢复
- **用户友好**：现代化响应式界面，支持移动端访问
- **功能完整**：从账号创建到数据备份的全生命周期管理

## ✨ 功能特性

### 🔐 安全认证
- **用户身份验证**：基于 ASP.NET Core Identity 的完整认证系统
- **密码策略**：强制密码复杂度要求，支持账户锁定机制
- **双因素认证**：TOTP 支持，生成二维码便于手机端配置
- **数据加密**：敏感数据使用 AES 加密存储
- **邮箱密码修改**：支持用户修改登录邮箱和密码

### 📊 账号管理
- **账号存储**：支持用户名、密码、网址、分类、标签等信息
- **安全问题**：为每个账号配置多个安全问题
- **访问记录**：自动记录账号访问历史和活动日志
- **批量操作**：支持账号的批量导入导出（CSV/Excel）
- **搜索筛选**：支持按关键词、分类、标签搜索账号
- **TOTP支持**：为账号生成双因素认证密钥

### 🔔 智能提醒
- **养号提醒**：基于Hangfire的定时任务，自动检测长期未访问的账号
- **自定义周期**：支持每日、每周、每月或自定义提醒周期
- **多通道通知**：支持邮件、Telegram、SignalR实时通知
- **提醒记录**：完整的提醒发送记录和状态跟踪
- **通知设置**：灵活配置各种通知渠道和模板

### 📈 数据统计
- **实时概览**：总账号数、活跃账号、需要提醒的账号统计
- **活动监控**：最近访问记录和操作日志
- **提醒统计**：提醒发送统计和成功率分析
- **数据可视化**：直观的统计图表和状态展示

### 🛠️ 系统管理
- **数据备份**：支持数据库的备份和恢复
- **任务调度**：基于 Hangfire + Quartz 的后台任务管理
- **日志记录**：基于Serilog的完整系统日志和错误跟踪
- **配置管理**：灵活的系统配置和参数设置
- **Hangfire仪表板**：可视化的任务调度管理界面

## 🚀 快速开始

### 系统要求

- **操作系统**：Windows 10/11, Linux, macOS
- **.NET SDK**：.NET 9.0 或更高版本
- **内存**：建议 4GB 以上
- **磁盘空间**：至少 1GB 可用空间

### 一键启动

#### Windows 用户（推荐）

1. **下载项目**
   ```bash
   git clone https://github.com/your-username/AccountMaintan.git
   cd AccountMaintan
   ```

2. **一键启动**
   - 右键点击 `build-and-run.ps1` 选择"使用 PowerShell 运行"
   - 或在 PowerShell 中执行：`.\build-and-run.ps1`

#### 手动启动

```bash
# 进入项目目录
cd src

# 还原依赖包
dotnet restore

# 编译项目
dotnet build --configuration Release

# 启动应用
dotnet run --configuration Release
```

### 首次使用

1. **访问系统**：浏览器打开 `https://localhost:7125`
2. **注册账户**：首次使用需要注册新账户
   - 点击"注册"按钮
   - 填写显示名称、邮箱和密码
   - 完成注册后自动登录
3. **开始使用**：登录后即可开始管理您的账号

## 📖 使用说明

### 用户认证

#### 注册账户
1. 访问登录页面，点击"注册"按钮
2. 填写显示名称、邮箱地址和密码
3. 完成注册后自动登录系统

#### 修改账户信息
1. 登录后点击"修改密码"菜单
2. 可以修改邮箱地址和密码
3. 修改邮箱后可使用新邮箱登录

### 账号管理

#### 创建账号
1. 点击"新增账号"按钮
2. 填写账号基本信息（名称、网址、用户名、密码）
3. 设置分类和标签便于管理
4. 配置养号提醒周期
5. 添加安全问题（可选）
6. 生成TOTP密钥（可选）
7. 保存账号信息

#### 查看密码
1. 在账号列表中点击"查看密码"
2. 系统会安全地显示解密后的密码
3. 支持一键复制到剪贴板

#### 双因素认证
1. 在账号编辑页面点击"生成TOTP"
2. 扫描二维码添加到手机认证器
3. 获取6位验证码用于登录

### 养号提醒

#### 设置提醒
1. 在账号编辑页面设置"提醒周期"
2. 选择提醒类型（每日/每周/每月/自定义）
3. 系统会自动检查并发送提醒

#### 查看提醒
1. 访问"提醒管理"页面
2. 查看需要提醒的账号列表
3. 手动记录访问或直接访问网站

#### 通知设置
1. 访问"系统设置"页面
2. 配置邮件、Telegram等通知渠道
3. 自定义通知模板和发送时间

### 数据管理

#### 导入导出
- **导出CSV**：支持CSV格式导出所有账号数据
- **导出Excel**：支持Excel格式导出，包含更多格式
- **导入CSV/Excel**：支持批量导入账号信息
- **模板下载**：提供标准导入模板

#### 备份恢复
- **数据备份**：定期备份数据库文件
- **数据恢复**：从备份文件恢复数据
- **安全**：备份文件包含加密的敏感数据

### 系统管理

#### 任务调度
1. 访问"Hangfire仪表板"查看任务状态
2. 监控提醒任务的执行情况
3. 手动触发提醒检查

#### 提醒记录
1. 访问"提醒记录"页面
2. 查看所有提醒发送记录
3. 分析提醒成功率和失败原因

## 🛠️ 技术栈

### 后端技术
- **框架**：ASP.NET Core 9.0
- **数据库**：SQLite + Entity Framework Core
- **ORM**：FreeSql 3.5.207
- **认证**：ASP.NET Core Identity
- **任务调度**：Hangfire 1.8.21 + Quartz 3.8.1
- **日志**：Serilog + File/Console Sinks
- **加密**：AES + System.Security.Cryptography
- **通知**：Gmail API + MimeKit + SignalR
- **文档处理**：ClosedXML (Excel)

### 前端技术
- **UI框架**：Bootstrap 5
- **图标**：Font Awesome
- **交互**：jQuery + SignalR
- **页面**：Razor Pages
- **实时通信**：ASP.NET Core SignalR

### 开发工具
- **IDE**：Visual Studio 2022 / VS Code
- **版本控制**：Git
- **包管理**：NuGet
- **构建**：MSBuild
- **运行时**：.NET 9.0

## 📁 项目结构

```
AccountMaintan/
├── src/                            # 源代码目录
│   ├── Controllers/                # API控制器
│   │   ├── AccountsController.cs   # 账号管理API
│   │   ├── RemindersController.cs  # 提醒管理API
│   │   ├── ReminderRecordsController.cs # 提醒记录API
│   │   ├── SettingsController.cs   # 系统设置API
│   │   └── HangfireSettingsController.cs # Hangfire设置API
│   ├── Data/                       # 数据访问层
│   │   ├── AppDbContext.cs         # 应用数据库上下文
│   │   └── IdentityDbContext.cs    # 身份认证数据库上下文
│   ├── Models/                     # 数据模型
│   │   ├── Account.cs              # 账号实体
│   │   ├── AccountActivity.cs      # 账号活动记录
│   │   ├── ApplicationUser.cs      # 用户实体
│   │   ├── SecurityQuestion.cs     # 安全问题实体
│   │   ├── ReminderRecord.cs       # 提醒记录实体
│   │   ├── NotificationSettings.cs # 通知设置
│   │   └── HangfireSettings.cs     # Hangfire设置
│   ├── Pages/                      # Razor页面
│   │   ├── Account/                # 用户认证页面
│   │   │   ├── Login.cshtml        # 登录页面
│   │   │   ├── Register.cshtml     # 注册页面
│   │   │   └── ChangePassword.cshtml # 修改密码页面
│   │   ├── Accounts/               # 账号管理页面
│   │   │   ├── Index.cshtml        # 账号列表
│   │   │   ├── Create.cshtml       # 创建账号
│   │   │   ├── Edit.cshtml         # 编辑账号
│   │   │   ├── Import.cshtml       # 导入账号
│   │   │   ├── Export.cshtml       # 导出账号
│   │   │   └── ExportExcel.cshtml  # 导出Excel
│   │   ├── Reminders/              # 提醒管理页面
│   │   ├── ReminderRecords/        # 提醒记录页面
│   │   ├── Settings/               # 系统设置页面
│   │   ├── Security/               # 安全设置页面
│   │   └── HangfireSettings/       # Hangfire设置页面
│   ├── Services/                   # 业务服务层
│   │   ├── AccountService.cs       # 账号业务逻辑
│   │   ├── EncryptionService.cs    # 加密服务
│   │   ├── TotpService.cs          # TOTP服务
│   │   ├── ReminderService.cs      # 提醒服务
│   │   ├── ReminderSchedulerService.cs # 提醒调度服务
│   │   ├── ReminderRecordService.cs # 提醒记录服务
│   │   ├── NotificationSettingsService.cs # 通知设置服务
│   │   └── DbInitializationService.cs # 数据库初始化服务
│   ├── Hubs/                       # SignalR集线器
│   │   └── ReminderHub.cs          # 实时通知
│   ├── Jobs/                       # 后台任务
│   │   └── ReminderCheckJob.cs     # 提醒检查任务
│   ├── Extensions/                 # 扩展方法
│   │   ├── HangfireExtensions.cs   # Hangfire扩展
│   │   └── QuartzExtensions.cs     # Quartz扩展
│   ├── Filters/                    # 过滤器
│   │   └── HangfireAuthorizationFilter.cs # Hangfire授权过滤器
│   ├── wwwroot/                    # 静态资源
│   │   ├── css/                    # 样式文件
│   │   ├── js/                     # JavaScript文件
│   │   └── lib/                    # 第三方库
│   ├── Logs/                       # 日志文件
│   ├── accounts.db                 # 主数据库
│   ├── identity.db                 # 身份认证数据库
│   ├── Program.cs                  # 程序入口
│   └── WebUI.csproj               # 项目文件
├── deploy.sh                       # 部署脚本
└── README.md                       # 项目说明文档
```

## 🚀 部署指南

### Windows 部署

#### IIS 部署
1. 发布应用程序
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. 配置 IIS
   - 创建应用程序池（.NET CLR版本：无托管代码）
   - 创建网站并指向发布目录
   - 配置HTTPS证书

#### 服务部署
1. 使用 NSSM 将应用注册为 Windows 服务
2. 配置自动启动和故障恢复
3. 设置日志目录权限

### Linux 部署

#### Docker 部署
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY ./publish /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "WebUI.dll"]
```

#### 系统服务
1. 创建 systemd 服务文件
2. 配置服务启动和重启策略
3. 设置防火墙规则

### 生产环境配置

#### 数据库配置
- 使用 PostgreSQL 或 SQL Server 替代 SQLite
- 配置连接池和性能优化
- 设置定期备份策略

#### 安全配置
- 配置 HTTPS 证书
- 设置强密码策略
- 启用安全头配置
- 配置防火墙规则

## 🤝 贡献指南

我们欢迎任何形式的贡献！

### 如何贡献
1. Fork 本仓库
2. 创建特性分支：`git checkout -b feature/AmazingFeature`
3. 提交更改：`git commit -m 'Add some AmazingFeature'`
4. 推送分支：`git push origin feature/AmazingFeature`
5. 提交 Pull Request

### 开发环境设置
1. 克隆仓库：`git clone https://github.com/your-username/AccountMaintan.git`
2. 安装 .NET 9.0 SDK
3. 还原依赖：`dotnet restore`
4. 启动应用：`dotnet run`

## 📝 更新日志

### v1.0.0 (2024-12)
- ✅ 完整的用户认证系统（注册、登录、邮箱密码修改）
- ✅ 账号密码管理功能（增删改查、搜索筛选）
- ✅ TOTP双因素认证支持
- ✅ 基于Hangfire的智能提醒系统
- ✅ 多通道通知（邮件、Telegram、SignalR）
- ✅ 数据导入导出（CSV/Excel）
- ✅ 安全加密存储（AES加密）
- ✅ 提醒记录和统计功能
- ✅ Hangfire任务调度仪表板
- ✅ 现代化响应式界面
- ✅ 完整的日志记录系统

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE.txt) 文件了解详情。

## 🙏 致谢

感谢以下开源项目的支持：
- [ASP.NET Core](https://github.com/dotnet/aspnetcore) - Web框架
- [FreeSql](https://github.com/dotnetcore/FreeSql) - ORM框架
- [Hangfire](https://github.com/HangfireIO/Hangfire) - 后台任务调度
- [Quartz](https://github.com/quartznet/quartznet) - 任务调度引擎
- [Serilog](https://github.com/serilog/serilog) - 结构化日志
- [Bootstrap](https://github.com/twbs/bootstrap) - UI框架
- [Font Awesome](https://github.com/FortAwesome/Font-Awesome) - 图标库
- [SignalR](https://github.com/dotnet/aspnetcore) - 实时通信
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) - Excel处理

## 📞 联系我们

- **项目主页**：https://github.com/ainasago/AccountMaintan
- **问题反馈**：https://github.com/ainasago/AccountMaintan/issues 

---

<div align="center">

**如果这个项目对您有帮助，请给我们一个 ⭐️**

Made with ❤️ by [Your Name]

</div>
