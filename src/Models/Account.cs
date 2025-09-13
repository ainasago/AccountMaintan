using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 账号实体
/// </summary>
public class Account
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 账号名称/服务名称
    /// </summary>
    [StringLength(100, ErrorMessage = "账号名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 登录网址
    /// </summary>
    [Url(ErrorMessage = "请输入有效的网址")]
    public string? Url { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码（加密存储）
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 到期时间（为空表示永久）
    /// </summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Notes { get; set; }

    /// <summary>
    /// TOTP 密钥
    /// </summary>
    public string? AuthenticatorKey { get; set; }

    /// <summary>
    /// 标签（逗号分隔）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
    public string? Category { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastVisited { get; set; }

    /// <summary>
    /// 提醒周期（天数）
    /// </summary>
    public int ReminderCycle { get; set; } = 30;

    /// <summary>
    /// 提醒类型
    /// </summary>
    public ReminderType ReminderType { get; set; } = ReminderType.Custom;

    /// <summary>
    /// 安全问题集合
    /// </summary>
    public virtual List<SecurityQuestion> SecurityQuestions { get; set; } = new();

    /// <summary>
    /// 账号活动记录
    /// </summary>
    public virtual List<AccountActivity> Activities { get; set; } = new();

    /// <summary>
    /// 获取距离上次访问的天数
    /// </summary>
    public int DaysSinceLastVisit
    {
        get
        {
            if (!LastVisited.HasValue)
                return int.MaxValue;
            
            return (int)(DateTime.Now - LastVisited.Value).TotalDays;
        }
    }
}

/// <summary>
/// 提醒类型枚举
/// </summary>
public enum ReminderType
{
    /// <summary>
    /// 每天
    /// </summary>
    Daily = 1,
    
    /// <summary>
    /// 每周
    /// </summary>
    Weekly = 7,
    
    /// <summary>
    /// 每月
    /// </summary>
    Monthly = 30,
    
    /// <summary>
    /// 自定义天数
    /// </summary>
    Custom = 0
}
