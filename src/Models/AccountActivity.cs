using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 账号活动记录实体
/// </summary>
public class AccountActivity
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 关联的账号ID
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// 活动类型
    /// </summary>
    public ActivityType ActivityType { get; set; }

    /// <summary>
    /// 活动描述
    /// </summary>
    [StringLength(500, ErrorMessage = "活动描述长度不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 活动时间
    /// </summary>
    public DateTime ActivityTime { get; set; } = DateTime.Now;

    /// <summary>
    /// IP地址（可选）
    /// </summary>
    [StringLength(45, ErrorMessage = "IP地址长度不能超过45个字符")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理（可选）
    /// </summary>
    [StringLength(500, ErrorMessage = "用户代理长度不能超过500个字符")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 关联的账号
    /// </summary>
    public virtual Account Account { get; set; } = null!;
}

/// <summary>
/// 活动类型枚举
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// 登录
    /// </summary>
    Login = 1,
    
    /// <summary>
    /// 密码修改
    /// </summary>
    PasswordChange = 2,
    
    /// <summary>
    /// 访问
    /// </summary>
    Visit = 3,
    
    /// <summary>
    /// 提醒
    /// </summary>
    Reminder = 4,
    
    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}
