namespace WebUI.Models;

/// <summary>
/// Hangfire 设置模型
/// </summary>
public class HangfireSettings
{
    /// <summary>
    /// 是否启用 Hangfire Dashboard
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Dashboard 访问路径
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = "admin";

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = "admin123";

    /// <summary>
    /// 是否启用基本认证
    /// </summary>
    public bool EnableBasicAuth { get; set; }
    public bool AllowAuthenticatedUsers { get; set; } = true;
}
