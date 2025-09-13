using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// 设置管理控制器
/// </summary>
[Authorize]
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly INotificationSettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        INotificationSettingsService settingsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取通知设置
    /// </summary>
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        _logger.LogDebug("用户 {User} 请求获取通知设置", User.Identity?.Name ?? "未知");
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            _logger.LogDebug("成功获取通知设置");
            return Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通知设置失败");
            return BadRequest(new { success = false, message = "获取设置失败" });
        }
    }

    /// <summary>
    /// 保存通知设置
    /// </summary>
    [HttpPost("notifications")]
    public async Task<IActionResult> SaveNotificationSettings([FromBody] NotificationSettings settings)
    {
        _logger.LogDebug("用户 {User} 请求保存通知设置", User.Identity?.Name ?? "未知");
        try
        {
            if (settings == null)
            {
                _logger.LogWarning("保存通知设置失败：设置数据为空");
                return BadRequest(new { success = false, message = "设置数据不能为空" });
            }

            _logger.LogDebug("开始保存通知设置，邮件启用: {EmailEnabled}, Telegram启用: {TelegramEnabled}", 
                settings.Email.IsEnabled, settings.Telegram.IsEnabled);

            var result = await _settingsService.SaveSettingsAsync(settings);
            if (result)
            {
                _logger.LogDebug("通知设置保存成功");
                return Ok(new { success = true, message = "设置保存成功" });
            }
            else
            {
                _logger.LogError("通知设置保存失败");
                return BadRequest(new { success = false, message = "设置保存失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存通知设置失败");
            return BadRequest(new { success = false, message = "保存设置失败" });
        }
    }

    /// <summary>
    /// 测试邮件配置
    /// </summary>
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmailSettings([FromBody] EmailSettings settings)
    {
        try
        {
            if (settings == null)
            {
                return BadRequest(new { success = false, message = "邮件设置不能为空" });
            }

            var result = await _settingsService.TestEmailSettingsAsync(settings);
            if (result)
            {
                return Ok(new { success = true, message = "邮件配置测试成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "邮件配置测试失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试邮件配置失败");
            return BadRequest(new { success = false, message = "测试失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 测试Telegram配置
    /// </summary>
    [HttpPost("test-telegram")]
    public async Task<IActionResult> TestTelegramSettings([FromBody] TelegramSettings settings)
    {
        try
        {
            if (settings == null)
            {
                return BadRequest(new { success = false, message = "Telegram设置不能为空" });
            }

            var result = await _settingsService.TestTelegramSettingsAsync(settings);
            if (result)
            {
                return Ok(new { success = true, message = "Telegram配置测试成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Telegram配置测试失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试Telegram配置失败");
            return BadRequest(new { success = false, message = "测试失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 测试所有通知
    /// </summary>
    [HttpPost("test-all")]
    public async Task<IActionResult> TestAllNotifications()
    {
        try
        {
            var result = await _settingsService.SendTestNotificationAsync();
            if (result)
            {
                return Ok(new { success = true, message = "所有通知测试成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "部分通知测试失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试所有通知失败");
            return BadRequest(new { success = false, message = "测试失败: " + ex.Message });
        }
    }
}
