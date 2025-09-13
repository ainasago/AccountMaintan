using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using WebUI.Models;

namespace WebUI.Filters;

/// <summary>
/// Hangfire 授权过滤器
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly HangfireSettings _settings;

    public HangfireAuthorizationFilter(IOptions<HangfireSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // 首先检查是否已经通过ASP.NET Core Identity认证
        if (httpContext.User.Identity?.IsAuthenticated == true && _settings.AllowAuthenticatedUsers)
        {
            // 如果已登录且允许认证用户访问，则允许访问
            return true;
        }

        // 如果未启用基本认证，则拒绝访问
        if (!_settings.EnableBasicAuth)
        {
            return false;
        }

        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            // 没有认证头或格式不正确，返回401要求认证
            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            httpContext.Response.StatusCode = 401;
            return false;
        }

        try
        {
            // 解码 Basic 认证
            var encodedCredentials = authHeader.Substring("Basic ".Length);
            var credentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = credentials.Split(':', 2);

            if (parts.Length != 2)
            {
                return false;
            }

            var username = parts[0];
            var password = parts[1];

            // 验证用户名和密码
            return username == _settings.Username && password == _settings.Password;
        }
        catch
        {
            return false;
        }
    }
}
