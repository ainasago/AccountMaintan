using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// CSP管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CspController : ControllerBase
{
    private readonly SecurityOptions _securityOptions;
    private readonly ICspValidationService _cspValidationService;
    private readonly ILogger<CspController> _logger;

    public CspController(
        IOptions<SecurityOptions> securityOptions,
        ICspValidationService cspValidationService,
        ILogger<CspController> logger)
    {
        _securityOptions = securityOptions.Value;
        _cspValidationService = cspValidationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前CSP配置
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetCspConfig()
    {
        try
        {
            var isDevelopment = HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;
            var cspOptions = isDevelopment ? _securityOptions.Development.ContentSecurityPolicy : _securityOptions.ContentSecurityPolicy;
            var cspString = cspOptions.ToCspString();

            return Ok(new
            {
                isDevelopment,
                cspString,
                config = cspOptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取CSP配置失败");
            return StatusCode(500, "获取CSP配置失败");
        }
    }

    /// <summary>
    /// 验证CSP配置
    /// </summary>
    [HttpPost("validate")]
    public IActionResult ValidateCsp([FromBody] string cspString)
    {
        try
        {
            var result = _cspValidationService.ValidateCsp(cspString);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证CSP配置失败");
            return StatusCode(500, "验证CSP配置失败");
        }
    }

    /// <summary>
    /// 获取CSP建议
    /// </summary>
    [HttpPost("recommendations")]
    public IActionResult GetCspRecommendations([FromBody] string cspString)
    {
        try
        {
            var recommendations = _cspValidationService.GetCspRecommendations(cspString);
            return Ok(new { recommendations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取CSP建议失败");
            return StatusCode(500, "获取CSP建议失败");
        }
    }

    /// <summary>
    /// 测试CSP违规
    /// </summary>
    [HttpPost("test-violation")]
    public IActionResult TestCspViolation()
    {
        try
        {
            // 这个端点故意返回可能触发CSP违规的内容
            var response = new
            {
                message = "CSP违规测试",
                script = "<script>alert('CSP违规测试')</script>",
                style = "<style>body{background:red}</style>"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSP违规测试失败");
            return StatusCode(500, "CSP违规测试失败");
        }
    }
}
