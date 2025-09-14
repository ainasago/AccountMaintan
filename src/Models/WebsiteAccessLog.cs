using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 网站访问日志实体
/// </summary>
[Table(Name = "WebsiteAccessLogs")]
public class WebsiteAccessLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [Column(IsPrimary = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属用户ID
    /// </summary>
    [Column(StringLength = 450, IsNullable = false)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 所属网站ID
    /// </summary>
    [Column(IsNullable = false)]
    public Guid WebsiteId { get; set; }

    /// <summary>
    /// 访问类型
    /// </summary>
    [Column(StringLength = 50, IsNullable = false)]
    public string AccessType { get; set; } = "Visit";

    /// <summary>
    /// 访问时间
    /// </summary>
    [Column(IsNullable = false)]
    public DateTime AccessTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 访问IP
    /// </summary>
    [Column(StringLength = 45, IsNullable = true)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 访问路径
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? AccessPath { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    [Column(IsNullable = true)]
    public int? StatusCode { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    [Column(IsNullable = true)]
    public long? ResponseTime { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [Column(StringLength = 1000, IsNullable = true)]
    public string? Notes { get; set; }

    /// <summary>
    /// 关联的网站
    /// </summary>
    public virtual Website? Website { get; set; }
}

/// <summary>
/// 访问类型枚举
/// </summary>
public enum AccessType
{
    /// <summary>
    /// 网站访问
    /// </summary>
    Visit,
    
    /// <summary>
    /// 服务重启
    /// </summary>
    Restart,
    
    /// <summary>
    /// 状态检查
    /// </summary>
    StatusCheck,
    
    /// <summary>
    /// 日志查看
    /// </summary>
    LogView,
    
    /// <summary>
    /// 其他操作
    /// </summary>
    Other
}

