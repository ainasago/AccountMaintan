using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 提醒记录模型
/// </summary>
[Table(Name = "ReminderRecords")]
public class ReminderRecord
{
    /// <summary>
    /// 记录ID
    /// </summary>
    [Column(IsPrimary = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 用户ID
    /// </summary>
    [Column(StringLength = 450, IsNullable = false)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 账号ID（测试时为null）
    /// </summary>
    [Column(DbType = "TEXT")]
    public Guid? AccountId { get; set; }

    /// <summary>
    /// 账号名称
    /// </summary>
    [Column(StringLength = 200, IsNullable = false)]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// 记录类型：Test（测试）、Reminder（实际提醒）
    /// </summary>
    [Column(StringLength = 20, IsNullable = false)]
    public string RecordType { get; set; } = "Reminder";

    /// <summary>
    /// 通知渠道：Email、Telegram、SignalR、All
    /// </summary>
    [Column(StringLength = 20, IsNullable = false)]
    public string NotificationChannel { get; set; } = "All";

    /// <summary>
    /// 发送状态：Success、Failed
    /// </summary>
    [Column(StringLength = 20, IsNullable = false)]
    public string Status { get; set; } = "Success";

    /// <summary>
    /// 发送的消息内容
    /// </summary>
    [Column(StringLength = 2000, IsNullable = true)]
    public string? Message { get; set; }

    /// <summary>
    /// 错误信息（如果发送失败）
    /// </summary>
    [Column(StringLength = 1000, IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 发送时间
    /// </summary>
    [Column(IsNullable = true)]
    public DateTime? SentAt { get; set; }
}
