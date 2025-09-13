using System.Security.Cryptography;
using System.Text;

namespace WebUI.Middleware;

/// <summary>
/// CSRF保护中间件
/// </summary>
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;
    private const string CsrfTokenName = "X-CSRF-TOKEN";
    private const string CsrfCookieName = "CSRF-TOKEN";

    public CsrfProtectionMiddleware(RequestDelegate next, ILogger<CsrfProtectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 跳过GET请求和静态文件
        if (context.Request.Method == "GET" || IsStaticFile(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // 跳过Razor Pages的POST请求（ASP.NET Core内置CSRF保护）
        if (IsRazorPageRequest(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // 只对API请求进行CSRF检查
        if (IsApiRequest(context.Request.Path))
        {
            // 检查CSRF令牌
            if (!await ValidateCsrfToken(context))
            {
                _logger.LogWarning("CSRF令牌验证失败: {Path} 来源: {RemoteIpAddress}", 
                    context.Request.Path, context.Connection.RemoteIpAddress);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("CSRF令牌验证失败");
                return;
            }
        }

        await _next(context);
    }

    private async Task<bool> ValidateCsrfToken(HttpContext context)
    {
        var request = context.Request;
        
        // 从请求头获取CSRF令牌
        var headerToken = request.Headers[CsrfTokenName].FirstOrDefault();
        
        // 从Cookie获取CSRF令牌
        var cookieToken = request.Cookies[CsrfCookieName] ?? "";

        if (string.IsNullOrEmpty(headerToken) || string.IsNullOrEmpty(cookieToken))
        {
            return false;
        }

        // 比较令牌
        if (!string.Equals(headerToken, cookieToken, StringComparison.Ordinal))
        {
            return false;
        }

        // 验证令牌格式和有效性
        return IsValidCsrfToken(headerToken);
    }

    private bool IsValidCsrfToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length != 64)
        {
            return false;
        }

        // 检查令牌是否只包含十六进制字符
        return token.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }

    private bool IsStaticFile(PathString path)
    {
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2" };
        return staticExtensions.Any(ext => path.Value.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsRazorPageRequest(PathString path)
    {
        // 检查是否是Razor Pages的POST请求（通常不包含/api/路径）
        return !path.Value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsApiRequest(PathString path)
    {
        // 检查是否是API请求
        return path.Value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// CSRF令牌服务
/// </summary>
public interface ICsrfTokenService
{
    /// <summary>
    /// 生成CSRF令牌
    /// </summary>
    string GenerateToken();
    
    /// <summary>
    /// 设置CSRF令牌Cookie
    /// </summary>
    void SetTokenCookie(HttpContext context, string token);
}

/// <summary>
/// CSRF令牌服务实现
/// </summary>
public class CsrfTokenService : ICsrfTokenService
{
    private readonly ILogger<CsrfTokenService> _logger;

    public CsrfTokenService(ILogger<CsrfTokenService> logger)
    {
        _logger = logger;
    }

    public string GenerateToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }

    public void SetTokenCookie(HttpContext context, string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(1)
        };

        context.Response.Cookies.Append("CSRF-TOKEN", token, cookieOptions);
        _logger.LogDebug("设置CSRF令牌Cookie");
    }
}
