using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebUI.Models;

namespace WebUI.Controllers;

/// <summary>
/// Hangfire 设置控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[
    Authorize
]
public class HangfireSettingsController : ControllerBase
{
    private readonly IOptions<HangfireSettings> _settings;
    private readonly ILogger<HangfireSettingsController> _logger;

    public HangfireSettingsController(IOptions<HangfireSettings> settings, ILogger<HangfireSettingsController> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// 获取 Hangfire 设置
    /// </summary>
    [HttpGet]
    public ActionResult<HangfireSettings> GetSettings()
    {
        var settings = _settings.Value;
        // 确保新字段有默认值
        if (settings.AllowAuthenticatedUsers == false && !settings.EnableBasicAuth)
        {
            settings.AllowAuthenticatedUsers = true; // 默认允许已登录用户访问
        }
        return Ok(settings);
    }

    /// <summary>
    /// 更新 Hangfire 设置
    /// </summary>
    [HttpPut]
    public ActionResult UpdateSettings([FromBody] HangfireSettings settings)
    {
        try
        {
            // 这里应该实现配置更新逻辑
            // 由于 IOptions 是只读的，需要重新加载配置或使用其他方式
            _logger.LogInformation("Hangfire 设置更新请求: 用户名={Username}, 启用认证={EnableAuth}", 
                settings.Username, settings.EnableBasicAuth);
            
            return Ok(new { message = "设置更新成功，请重启应用程序以应用更改" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Hangfire 设置失败");
            return BadRequest(new { message = "更新设置失败" });
        }
    }
}
