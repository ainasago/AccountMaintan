using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 管理员设置模型
/// </summary>
public class AdminSettings
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 是否允许用户注册
    /// </summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>
    /// 是否需要管理员批准新用户
    /// </summary>
    public bool RequireAdminApproval { get; set; } = false;

    /// <summary>
    /// 最大用户数量（0表示无限制）
    /// </summary>
    public int MaxUsers { get; set; } = 0;

    /// <summary>
    /// 默认用户角色
    /// </summary>
    public string DefaultUserRole { get; set; } = "User";

    /// <summary>
    /// 系统维护模式
    /// </summary>
    public bool MaintenanceMode { get; set; } = false;

    /// <summary>
    /// 维护模式消息
    /// </summary>
    public string MaintenanceMessage { get; set; } = "系统正在维护中，请稍后再试。";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新者ID
    /// </summary>
    public string? UpdatedBy { get; set; }
}
