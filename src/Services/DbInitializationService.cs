using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 数据库初始化服务实现
/// </summary>
public class DbInitializationService : IDbInitializationService
{
    private readonly ApplicationDbContext _identityContext;
    private readonly AppDbContext _appContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DbInitializationService> _logger;

    public DbInitializationService(
        ApplicationDbContext identityContext,
        AppDbContext appContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DbInitializationService> logger)
    {
        _identityContext = identityContext;
        _appContext = appContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // 先初始化FreeSql数据库（创建应用表）
            _appContext.InitializeDatabase();
            
            // 然后应用Identity迁移（创建Identity表）
            await _identityContext.Database.MigrateAsync();

            // 创建默认角色
            await CreateDefaultRolesAsync();

            // 创建默认用户
            await CreateDefaultUserAsync();

            _logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

    private async Task CreateDefaultRolesAsync()
    {
        string[] roleNames = { "Admin", "User" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("创建角色: {RoleName}", roleName);
            }
        }
    }

    private async Task CreateDefaultUserAsync()
    {
        // 检查是否已有用户
        var usersCount = await _userManager.Users.CountAsync();
        if (usersCount > 0)
        {
            return; // 已有用户，跳过创建
        }

        // 创建默认管理员用户
        var adminUser = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            DisplayName = "管理员",
            EmailConfirmed = true,
            IsEnabled = true,
            IsAdmin = true,
            IsSuperAdmin = true, // 第一个用户是超级管理员
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(adminUser, "Admin123qweasd!");
        
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
            _logger.LogInformation("创建默认管理员用户: {Email}", adminUser.Email);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("创建默认用户失败: {Errors}", errors);
        }
    }
}
