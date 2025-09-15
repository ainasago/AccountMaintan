using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;
using WebUI.Middleware;

namespace WebUI.Controllers;

/// <summary>
/// 管理员API控制器
/// </summary>
[Authorize]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;
    private readonly IPasswordEncryptionService _passwordEncryptionService;

    public AdminController(
        IAdminService adminService, 
        ILogger<AdminController> logger,
        IPasswordEncryptionService passwordEncryptionService)
    {
        _adminService = adminService;
        _logger = logger;
        _passwordEncryptionService = passwordEncryptionService;
    }

    /// <summary>
    /// 更新用户（可选修改密码）
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "用户不存在" });
            }

            // 不能修改超级管理员的管理员位（但允许更新显示名称/密码）
            if (user.IsSuperAdmin && request.IsAdmin.HasValue && request.IsAdmin.Value == false)
            {
                return BadRequest(new { success = false, message = "不能撤销超级管理员权限" });
            }

            // 更新可变更的字段
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.DisplayName = request.DisplayName.Trim();
            }
            if (request.IsAdmin.HasValue)
            {
                user.IsAdmin = request.IsAdmin.Value;
            }

            // 处理可选的密码修改
            if (!string.IsNullOrWhiteSpace(request.EncryptedNewPassword) || !string.IsNullOrWhiteSpace(request.EncryptedConfirmPassword))
            {
                if (string.IsNullOrWhiteSpace(request.EncryptedNewPassword) || string.IsNullOrWhiteSpace(request.EncryptedConfirmPassword) || string.IsNullOrWhiteSpace(request.EncryptionToken))
                {
                    return BadRequest(new { success = false, message = "密码修改需要提供加密后的新密码、确认密码以及令牌" });
                }

                string newPassword;
                string confirmPassword;
                try
                {
                    newPassword = _passwordEncryptionService.DecryptPassword(request.EncryptedNewPassword, request.EncryptionToken);
                    confirmPassword = _passwordEncryptionService.DecryptPassword(request.EncryptedConfirmPassword, request.EncryptionToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "编辑用户解密新密码失败");
                    return BadRequest(new { success = false, message = "新密码解密失败，请刷新页面重试" });
                }

                if (newPassword != confirmPassword)
                {
                    return BadRequest(new { success = false, message = "新密码与确认密码不一致" });
                }

                // 使用Identity重置密码（无需旧密码）
                try
                {
                    // 先移除现有密码（如果有）
                    var removeResult = await HttpContext.RequestServices
                        .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>()
                        .RemovePasswordAsync(user);
                    // 无密码时Remove可能失败，忽略

                    var addResult = await HttpContext.RequestServices
                        .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>()
                        .AddPasswordAsync(user, newPassword);
                    if (!addResult.Succeeded)
                    {
                        var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                        return BadRequest(new { success = false, message = $"修改密码失败: {errors}" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "设置用户新密码失败");
                    return StatusCode(500, new { success = false, message = "设置新密码时发生错误" });
                }
            }

            var updateResult = await _adminService.UpdateUserAsync(user);
            return Ok(new { success = updateResult.success, message = updateResult.message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败");
            return StatusCode(500, new { success = false, message = "更新用户失败" });
        }
    }
    /// <summary>
    /// 获取所有用户
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var users = await _adminService.GetAllUsersAsync();
            return Ok(new { success = true, data = users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return StatusCode(500, new { success = false, message = "获取用户列表失败" });
        }
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "用户不存在" });
            }

            return Ok(new { success = true, data = new {
                user.Id,
                user.DisplayName,
                user.Email,
                user.IsAdmin,
                user.IsEnabled,
                user.CreatedAt,
                user.LastLoginAt
            }});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户失败: {UserId}", id);
            return StatusCode(500, new { success = false, message = "获取用户失败" });
        }
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            // 解密密码和确认密码
            string decryptedPassword;
            string decryptedConfirmPassword;
            try
            {
                _logger.LogInformation("开始解密密码，加密密码长度: {Length}, 令牌长度: {TokenLength}", 
                    request.EncryptedPassword?.Length ?? 0, request.EncryptionToken?.Length ?? 0);
                
                decryptedPassword = _passwordEncryptionService.DecryptPassword(request.EncryptedPassword, request.EncryptionToken);
                decryptedConfirmPassword = _passwordEncryptionService.DecryptPassword(request.EncryptedConfirmPassword, request.EncryptionToken);
                
                _logger.LogInformation("密码解密成功，解密后长度: {Length}", decryptedPassword?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密码解密失败，加密密码: {EncryptedPassword}, 令牌: {Token}", 
                    request.EncryptedPassword, request.EncryptionToken);
                return BadRequest(new { success = false, message = "密码解密失败，请刷新页面重试" });
            }

            if (decryptedPassword != decryptedConfirmPassword)
            {
                return BadRequest(new { success = false, message = "密码和确认密码不匹配" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName,
                IsAdmin = request.IsAdmin,
                CreatedAt = DateTime.Now
            };

            var result = await _adminService.CreateUserAsync(user, decryptedPassword);
            return Ok(new { success = result.success, message = result.message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败: {Message}", ex.Message);
            return StatusCode(500, new { success = false, message = $"创建用户失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 切换用户状态
    /// </summary>
    [HttpPost("users/{id}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var result = await _adminService.ToggleUserStatusAsync(id);
            return Ok(new { success = result.success, message = result.message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户状态失败");
            return StatusCode(500, new { success = false, message = "切换用户状态失败" });
        }
    }

    /// <summary>
    /// 切换管理员状态
    /// </summary>
    [HttpPost("users/{id}/toggle-admin")]
    public async Task<IActionResult> ToggleAdminStatus(string id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var result = await _adminService.ToggleAdminStatusAsync(id);
            return Ok(new { success = result.success, message = result.message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换管理员状态失败");
            return StatusCode(500, new { success = false, message = "切换管理员状态失败" });
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var result = await _adminService.DeleteUserAsync(id);
            return Ok(new { success = result.success, message = result.message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户失败");
            return StatusCode(500, new { success = false, message = "删除用户失败" });
        }
    }

    /// <summary>
    /// 获取管理员设置
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var settings = await _adminService.GetAdminSettingsAsync();
            return Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取管理员设置失败");
            return StatusCode(500, new { success = false, message = "获取管理员设置失败" });
        }
    }

    /// <summary>
    /// 获取CSRF令牌
    /// </summary>
    [HttpGet("csrf-token")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        try
        {
            var csrfTokenService = HttpContext.RequestServices.GetService<ICsrfTokenService>();
            if (csrfTokenService == null)
            {
                return Ok(new { success = false, message = "CSRF服务不可用" });
            }

            var token = csrfTokenService.GenerateToken();
            csrfTokenService.SetTokenCookie(HttpContext, token);
            
            return Ok(new { success = true, token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取CSRF令牌失败");
            return StatusCode(500, new { success = false, message = "获取CSRF令牌失败" });
        }
    }

    /// <summary>
    /// 获取密码加密令牌（管理员专用）
    /// </summary>
    [HttpGet("password-encryption-token")]
    public IActionResult GetPasswordEncryptionToken()
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !_adminService.IsAdminAsync(currentUserId).Result)
            {
                return Forbid();
            }

            var token = _passwordEncryptionService.GenerateEncryptionToken();
            var expiryTime = _passwordEncryptionService.GetTokenExpiryTime(token);
            
            return Ok(new { 
                success = true, 
                token = token,
                expiresAt = expiryTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取密码加密令牌失败");
            return StatusCode(500, new { success = false, message = "获取密码加密令牌失败" });
        }
    }

    /// <summary>
    /// 获取密码加密令牌（普通用户专用）
    /// </summary>
    [HttpGet("user/password-encryption-token")]
    public IActionResult GetUserPasswordEncryptionToken()
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var token = _passwordEncryptionService.GenerateEncryptionToken();
            var expiryTime = _passwordEncryptionService.GetTokenExpiryTime(token);
            
            return Ok(new { 
                success = true, 
                token = token,
                expiresAt = expiryTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户密码加密令牌失败");
            return StatusCode(500, new { success = false, message = "获取密码加密令牌失败" });
        }
    }

    /// <summary>
    /// 获取密码加密令牌（匿名访问，用于登录页面）
    /// </summary>
    [HttpGet("anonymous/password-encryption-token")]
    [AllowAnonymous]
    public IActionResult GetAnonymousPasswordEncryptionToken()
    {
        try
        {
            var token = _passwordEncryptionService.GenerateEncryptionToken();
            var expiryTime = _passwordEncryptionService.GetTokenExpiryTime(token);
            
            return Ok(new { 
                success = true, 
                token = token,
                expiresAt = expiryTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取匿名密码加密令牌失败");
            return StatusCode(500, new { success = false, message = "获取密码加密令牌失败" });
        }
    }

    /// <summary>
    /// 检查用户权限
    /// </summary>
    [HttpGet("check-permissions")]
    public async Task<IActionResult> CheckPermissions()
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                // 用户未登录，返回未认证状态
                return Unauthorized(new { isAdmin = false, message = "用户未登录" });
            }

            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            return Ok(new { isAdmin = isAdmin });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户权限失败");
            return Ok(new { isAdmin = false });
        }
    }

    /// <summary>
    /// 更新管理员设置
    /// </summary>
    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] AdminSettings settings)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            var result = await _adminService.UpdateAdminSettingsAsync(settings, currentUserId);
            return Ok(new { success = result.success, message = result.message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新管理员设置失败");
            return StatusCode(500, new { success = false, message = "更新管理员设置失败" });
        }
    }
}

/// <summary>
/// 创建用户请求模型
/// </summary>
public class CreateUserRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string EncryptedConfirmPassword { get; set; } = string.Empty;
    public string EncryptionToken { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
}

/// <summary>
/// 更新用户请求模型
/// </summary>
public class UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public bool? IsAdmin { get; set; }

    // 可选：修改密码（加密传输）
    public string? EncryptedNewPassword { get; set; }
    public string? EncryptedConfirmPassword { get; set; }
    public string? EncryptionToken { get; set; }
}
