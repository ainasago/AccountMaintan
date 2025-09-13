using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;
using WebUI.Extensions;
using WebUI.Hubs;
using WebUI.Models;
using WebUI.Services;
using WebUI.Validators;
using WebUI.Middleware;
using FreeSql;
using Serilog;
using Serilog.Events;

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Filter.ByExcluding(e => e.Level == LogEventLevel.Information) // 排除 Information 级别
    .Filter.ByExcluding(e => e.Properties.ContainsKey("SourceContext") && 
        (e.Properties["SourceContext"].ToString().StartsWith("\"Microsoft") || 
         e.Properties["SourceContext"].ToString().StartsWith("\"System"))) // 排除 Microsoft 和 System 日志
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("Logs", "app-.log"),
        restrictedToMinimumLevel: LogEventLevel.Debug, // 记录 Debug 及以上级别
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
        rollOnFileSizeLimit: true,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("启动应用程序...");
    
    var builder = WebApplication.CreateBuilder(args);

    // 配置 ASP.NET Core 使用 Serilog
    builder.Host.UseSerilog();

    // 检测是否为桌面模式
    bool isDesktop = args.Any(a => string.Equals(a, "--desktop", StringComparison.OrdinalIgnoreCase));

// Add services to the container.
builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();

// 添加控制器服务
builder.Services.AddControllers();


// 在反向代理场景下配置HTTPS重定向与转发头
var httpsPort = builder.Configuration.GetValue<int?>("Https:Port") ;
if (httpsPort.HasValue)
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.HttpsPort = httpsPort;
    });
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    });
}

// 添加数据库上下文
builder.Services.AddSingleton<AppDbContext>();

// 配置FreeSql（与 DefaultConnection 使用同一数据库）
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=accounts.db";
string ResolveSqlitePath(string conn)
{
    const string prefix = "Data Source=";
    var idx = conn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
    if (idx >= 0)
    {
        var path = conn.Substring(idx + prefix.Length).Trim().Trim('"');
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }
        return $"Data Source={path};Version=3;";
    }
    // 如果不是标准格式，直接返回原连接串
    return conn;
}
var sqliteConn = ResolveSqlitePath(defaultConn);
var freeSql = new FreeSqlBuilder()
    .UseConnectionString(DataType.Sqlite, sqliteConn)
    .UseAutoSyncStructure(true)
    .Build();
builder.Services.AddSingleton<IFreeSql>(freeSql);

// 添加Identity数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("IdentityConnection")));

// 添加Identity服务
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // 密码配置 - 强化安全策略
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;

    // 锁定配置
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // 用户配置
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // 登录配置
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddPasswordValidator<CustomPasswordValidator<ApplicationUser>>();

// 配置Cookie - 强化安全设置
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2); // 缩短到2小时
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true; // 防止XSS攻击
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 仅HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict; // 防止CSRF攻击
    options.Cookie.Name = "AccountManager.Auth"; // 自定义Cookie名称
});

// 添加加密服务
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// 添加密码验证服务
builder.Services.AddScoped<IPasswordValidatorService, PasswordValidatorService>();

// 添加安全加密服务
builder.Services.AddScoped<ISecureEncryptionService, SecureEncryptionService>();

// 添加CSRF令牌服务
builder.Services.AddScoped<ICsrfTokenService, CsrfTokenService>();

// 添加密码加密服务
builder.Services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();

// 添加内存缓存（用于频率限制）
builder.Services.AddMemoryCache();

// 添加TOTP服务
builder.Services.AddScoped<ITotpService, TotpService>();

// 添加账号服务
builder.Services.AddScoped<IAccountService, AccountService>();

// 添加提醒服务
builder.Services.AddScoped<IReminderService, ReminderService>();

// 添加提醒通知服务
builder.Services.AddScoped<WebUI.Services.IReminderNotificationService, WebUI.Services.ReminderNotificationService>();

// 添加提醒调度服务（单例）
builder.Services.AddSingleton<IReminderSchedulerService, ReminderSchedulerService>();

// 添加通知设置服务
builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();

// 添加提醒记录服务
builder.Services.AddScoped<IReminderRecordService, ReminderRecordService>();

// 添加管理员服务
builder.Services.AddScoped<IAdminService, AdminService>();

// 添加数据库初始化服务
builder.Services.AddScoped<IDbInitializationService, DbInitializationService>();

// 添加SignalR
builder.Services.AddSignalR();

// 添加Hangfire服务
builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 先处理来自代理的转发头，再做HTTPS重定向
app.UseForwardedHeaders();
if (!isDesktop)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// 添加安全中间件
app.UseMiddleware<ApiSecurityMiddleware>();
app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 映射控制器路由
app.MapControllers();

// 配置Hangfire Dashboard
app.UseHangfireDashboard(builder.Configuration);

// 映射SignalR Hub
app.MapHub<ReminderHub>("/reminderHub");

app.MapRazorPages();

// 初始化数据库和启动提醒调度
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<IDbInitializationService>();
    await dbInitService.InitializeAsync();
    
    // 启动提醒调度
    var reminderScheduler = scope.ServiceProvider.GetRequiredService<IReminderSchedulerService>();
    await reminderScheduler.StartReminderSchedulerAsync();
    
    // 为每个现有用户启动独立的提醒调度
    var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
    var allAccounts = await accountService.GetAllAccountsAsync();
    var userIds = allAccounts.Select(a => a.UserId).Distinct().ToList();
    
    foreach (var userId in userIds)
    {
        try
        {
            await reminderScheduler.StartUserReminderSchedulerAsync(userId);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "为用户 {UserId} 启动提醒调度失败", userId);
        }
    }
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
}
finally
{
    Log.CloseAndFlush();
}
