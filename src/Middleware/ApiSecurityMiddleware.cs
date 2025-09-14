using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using WebUI.Models;

namespace WebUI.Middleware;

/// <summary>
/// API安全中间件
/// </summary>
public class ApiSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiSecurityMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly SecurityOptions _securityOptions;

    public ApiSecurityMiddleware(RequestDelegate next, ILogger<ApiSecurityMiddleware> logger, IConfiguration configuration, IOptions<SecurityOptions> securityOptions)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _securityOptions = securityOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 添加安全头
        AddSecurityHeaders(context);

        // 检查请求大小限制
        if (await CheckRequestSizeLimit(context))
        {
            return;
        }

        // 检查API访问频率
        if (await CheckRateLimit(context))
        {
            return;
        }

        // 记录API访问
        LogApiAccess(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        // 防止MIME类型嗅探
        response.Headers["X-Content-Type-Options"] = "nosniff";

        // 防止点击劫持
        response.Headers["X-Frame-Options"] = "DENY";

        // XSS保护
        response.Headers["X-XSS-Protection"] = "1; mode=block";

        // 内容安全策略 - 从配置文件读取
        var cspOptions = GetCspOptions(context);
        var csp = cspOptions.ToCspString();
        
        response.Headers["Content-Security-Policy"] = csp;

        // 严格传输安全（仅HTTPS）
        if (context.Request.IsHttps)
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // 引用者策略
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // 权限策略
        response.Headers["Permissions-Policy"] = 
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "speaker=()";
    }

    /// <summary>
    /// 获取CSP配置选项
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>CSP配置选项</returns>
    private ContentSecurityPolicyOptions GetCspOptions(HttpContext context)
    {
        var isDevelopment = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;
        
        if (isDevelopment)
        {
            return _securityOptions.Development.ContentSecurityPolicy;
        }
        
        return _securityOptions.ContentSecurityPolicy;
    }

    private async Task<bool> CheckRequestSizeLimit(HttpContext context)
    {
        const long maxRequestSize = 10 * 1024 * 1024; // 10MB

        if (context.Request.ContentLength > maxRequestSize)
        {
            _logger.LogWarning("请求大小超限: {ContentLength} bytes, 来源: {RemoteIpAddress}", 
                context.Request.ContentLength, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 413; // Payload Too Large
            await context.Response.WriteAsync("请求大小超过限制");
            return true;
        }

        return false;
    }

    private async Task<bool> CheckRateLimit(HttpContext context)
    {
        // 简单的内存缓存实现（生产环境建议使用Redis）
        var cache = context.RequestServices.GetService<IMemoryCache>();
        if (cache == null) return false;

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"rate_limit_{clientIp}";
        var windowStart = DateTime.UtcNow.AddMinutes(-1);
        
        if (cache.TryGetValue(cacheKey, out List<DateTime>? requests))
        {
            // 清理过期的请求记录
            requests.RemoveAll(r => r < windowStart);
            
            // 检查是否超过限制（每分钟100次请求）
            if (requests.Count >= 100)
            {
                _logger.LogWarning("API访问频率超限: {ClientIp}, 请求次数: {Count}", clientIp, requests.Count);
                
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("请求过于频繁，请稍后再试");
                return true;
            }
            
            requests.Add(DateTime.UtcNow);
        }
        else
        {
            cache.Set(cacheKey, new List<DateTime> { DateTime.UtcNow }, TimeSpan.FromMinutes(2));
        }

        return false;
    }

    private void LogApiAccess(HttpContext context)
    {
        var request = context.Request;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
        var userId = context.User?.Identity?.Name ?? "anonymous";

        _logger.LogInformation("API访问: {Method} {Path} 来源: {ClientIp} 用户: {UserId} UserAgent: {UserAgent}",
            request.Method, request.Path, clientIp, userId, userAgent);
    }
}
