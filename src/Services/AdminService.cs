using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 管理员服务接口
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<List<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<ApplicationUser?> GetUserByIdAsync(string id);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<(bool success, string message)> CreateUserAsync(ApplicationUser user, string password);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<(bool success, string message)> UpdateUserAsync(ApplicationUser user);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<(bool success, string message)> DeleteUserAsync(string id);

    /// <summary>
    /// 启用/禁用用户
    /// </summary>
    Task<(bool success, string message)> ToggleUserStatusAsync(string id);

    /// <summary>
    /// 设置/取消管理员权限
    /// </summary>
    Task<(bool success, string message)> ToggleAdminStatusAsync(string id);

    /// <summary>
    /// 获取管理员设置
    /// </summary>
    Task<AdminSettings> GetAdminSettingsAsync();

    /// <summary>
    /// 更新管理员设置
    /// </summary>
    Task<(bool success, string message)> UpdateAdminSettingsAsync(AdminSettings settings, string updatedBy);

    /// <summary>
    /// 检查是否为管理员
    /// </summary>
    Task<bool> IsAdminAsync(string userId);

    /// <summary>
    /// 检查是否为超级管理员
    /// </summary>
    Task<bool> IsSuperAdminAsync(string userId);

    /// <summary>
    /// 检查是否允许注册
    /// </summary>
    Task<bool> IsRegistrationAllowedAsync();
}

/// <summary>
/// 管理员服务实现
/// </summary>
public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<AdminService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        try
        {
            return await _userManager.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            throw;
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        try
        {
            return await _userManager.FindByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户失败: {UserId}", id);
            throw;
        }
    }

    public async Task<(bool success, string message)> CreateUserAsync(ApplicationUser user, string password)
    {
        try
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                _logger.LogInformation("成功创建用户: {Email}", user.Email);
                return (true, "用户创建成功");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("创建用户失败: {Email}, 错误: {Errors}", user.Email, errors);
                return (false, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户异常: {Email}", user.Email);
            return (false, "创建用户时发生错误");
        }
    }

    public async Task<(bool success, string message)> UpdateUserAsync(ApplicationUser user)
    {
        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("成功更新用户: {Email}", user.Email);
                return (true, "用户更新成功");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("更新用户失败: {Email}, 错误: {Errors}", user.Email, errors);
                return (false, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户异常: {Email}", user.Email);
            return (false, "更新用户时发生错误");
        }
    }

    public async Task<(bool success, string message)> DeleteUserAsync(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            // 不能删除超级管理员
            if (user.IsSuperAdmin)
            {
                return (false, "不能删除超级管理员");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("成功删除用户: {Email}", user.Email);
                return (true, "用户删除成功");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("删除用户失败: {Email}, 错误: {Errors}", user.Email, errors);
                return (false, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户异常: {UserId}", id);
            return (false, "删除用户时发生错误");
        }
    }

    public async Task<(bool success, string message)> ToggleUserStatusAsync(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            // 不能禁用超级管理员
            if (user.IsSuperAdmin)
            {
                return (false, "不能禁用超级管理员");
            }

            user.IsEnabled = !user.IsEnabled;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var status = user.IsEnabled ? "启用" : "禁用";
                _logger.LogInformation("成功{Status}用户: {Email}", status, user.Email);
                return (true, $"用户{status}成功");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户状态异常: {UserId}", id);
            return (false, "切换用户状态时发生错误");
        }
    }

    public async Task<(bool success, string message)> ToggleAdminStatusAsync(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            // 不能修改超级管理员权限
            if (user.IsSuperAdmin)
            {
                return (false, "不能修改超级管理员权限");
            }

            user.IsAdmin = !user.IsAdmin;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var status = user.IsAdmin ? "授予" : "撤销";
                _logger.LogInformation("成功{Status}管理员权限: {Email}", status, user.Email);
                return (true, $"管理员权限{status}成功");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换管理员状态异常: {UserId}", id);
            return (false, "切换管理员状态时发生错误");
        }
    }

    public async Task<AdminSettings> GetAdminSettingsAsync()
    {
        try
        {
            var settings = await _context.AdminSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // 创建默认设置
                settings = new AdminSettings();
                _context.AdminSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取管理员设置失败");
            return new AdminSettings();
        }
    }

    public async Task<(bool success, string message)> UpdateAdminSettingsAsync(AdminSettings settings, string updatedBy)
    {
        try
        {
            var existingSettings = await _context.AdminSettings.FirstOrDefaultAsync();
            if (existingSettings == null)
            {
                settings.UpdatedBy = updatedBy;
                settings.LastUpdated = DateTime.Now;
                _context.AdminSettings.Add(settings);
            }
            else
            {
                existingSettings.AllowRegistration = settings.AllowRegistration;
                existingSettings.RequireAdminApproval = settings.RequireAdminApproval;
                existingSettings.MaxUsers = settings.MaxUsers;
                existingSettings.DefaultUserRole = settings.DefaultUserRole;
                existingSettings.MaintenanceMode = settings.MaintenanceMode;
                existingSettings.MaintenanceMessage = settings.MaintenanceMessage;
                existingSettings.UpdatedBy = updatedBy;
                existingSettings.LastUpdated = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("管理员设置已更新: {UpdatedBy}", updatedBy);
            return (true, "设置更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新管理员设置失败");
            return (false, "更新设置时发生错误");
        }
    }

    public async Task<bool> IsAdminAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsAdmin == true || user?.IsSuperAdmin == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查管理员权限失败: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsSuperAdminAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsSuperAdmin == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查超级管理员权限失败: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsRegistrationAllowedAsync()
    {
        try
        {
            var settings = await GetAdminSettingsAsync();
            return settings.AllowRegistration && !settings.MaintenanceMode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查注册权限失败");
            return true; // 默认允许注册
        }
    }
}
