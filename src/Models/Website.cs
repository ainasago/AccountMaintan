using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 网站实体
/// </summary>
[Table(Name = "Websites")]
public class Website
{
    /// <summary>
    /// 网站ID
    /// </summary>
    [Column(IsPrimary = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属用户ID
    /// </summary>
    [Column(StringLength = 450, IsNullable = false)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 所属服务器ID
    /// </summary>
    [Column(IsNullable = false)]
    public Guid ServerId { get; set; }

    /// <summary>
    /// 网站名称
    /// </summary>
    [Column(StringLength = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 网站描述
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    [Column(StringLength = 255, IsNullable = false)]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    [Column(IsNullable = false)]
    public int Port { get; set; } = 80;

    /// <summary>
    /// 是否使用HTTPS
    /// </summary>
    [Column(IsNullable = false)]
    public bool UseHttps { get; set; } = false;

    /// <summary>
    /// 网站路径
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? WebPath { get; set; }

    /// <summary>
    /// Supervisor进程名称
    /// </summary>
    [Column(StringLength = 100, IsNullable = true)]
    public string? SupervisorProcessName { get; set; }

    /// <summary>
    /// 网站状态
    /// </summary>
    [Column(StringLength = 20, IsNullable = false)]
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// 是否启用
    /// </summary>
    [Column(IsNullable = false)]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后检查时间
    /// </summary>
    [Column(IsNullable = true)]
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [Column(IsNullable = true)]
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [Column(StringLength = 1000, IsNullable = true)]
    public string? Notes { get; set; }

    /// <summary>
    /// 标签（逗号分隔）
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? Tags { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    [Column(StringLength = 50, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>
    /// 关联的服务器
    /// </summary>
    public virtual Server? Server { get; set; }

    /// <summary>
    /// 网站账号列表
    /// </summary>
    public virtual List<WebsiteAccount> WebsiteAccounts { get; set; } = new();

    /// <summary>
    /// 网站访问日志
    /// </summary>
    public virtual List<WebsiteAccessLog> AccessLogs { get; set; } = new();

    /// <summary>
    /// 获取完整URL
    /// </summary>
    public string FullUrl
    {
        get
        {
            var protocol = UseHttps ? "https" : "http";
            var port = (UseHttps && Port == 443) || (!UseHttps && Port == 80) ? "" : $":{Port}";
            return $"{protocol}://{Domain}{port}";
        }
    }
}

/// <summary>
/// 网站状态枚举
/// </summary>
public enum WebsiteStatus
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 运行中
    /// </summary>
    Running,
    
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,
    
    /// <summary>
    /// 启动中
    /// </summary>
    Starting,
    
    /// <summary>
    /// 已退出
    /// </summary>
    Exited,
    
    /// <summary>
    /// 异常状态
    /// </summary>
    Error
}

