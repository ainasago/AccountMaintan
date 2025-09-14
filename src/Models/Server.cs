using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 服务器实体
/// </summary>
[Table(Name = "Servers")]
public class Server
{
    /// <summary>
    /// 服务器ID
    /// </summary>
    [Column(IsPrimary = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属用户ID
    /// </summary>
    [Column(StringLength = 450, IsNullable = false)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 服务器名称
    /// </summary>
    [Column(StringLength = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 服务器描述
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [Column(StringLength = 45, IsNullable = false)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// SSH端口
    /// </summary>
    [Column(IsNullable = false)]
    public int SshPort { get; set; } = 22;

    /// <summary>
    /// SSH用户名
    /// </summary>
    [Column(StringLength = 100, IsNullable = false)]
    public string SshUsername { get; set; } = string.Empty;

    /// <summary>
    /// SSH密码（加密存储）
    /// </summary>
    [Column(StringLength = 1000, IsNullable = true)]
    public string? SshPassword { get; set; }

    /// <summary>
    /// SSH私钥路径
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? SshPrivateKeyPath { get; set; }

    /// <summary>
    /// 操作系统类型
    /// </summary>
    [Column(StringLength = 50, IsNullable = true)]
    public string? OperatingSystem { get; set; }

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
    /// 最后连接时间
    /// </summary>
    [Column(IsNullable = true)]
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    [Column(StringLength = 20, IsNullable = false)]
    public string ConnectionStatus { get; set; } = "Unknown";

    /// <summary>
    /// 备注信息
    /// </summary>
    [Column(StringLength = 1000, IsNullable = true)]
    public string? Notes { get; set; }

    /// <summary>
    /// 关联的网站列表
    /// </summary>
    public virtual List<Website> Websites { get; set; } = new();

    /// <summary>
    /// 资源使用情况记录
    /// </summary>
    public virtual List<ServerResourceUsage> ResourceUsages { get; set; } = new();
}

/// <summary>
/// 连接状态枚举
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 连接中
    /// </summary>
    Connecting,
    
    /// <summary>
    /// 已连接
    /// </summary>
    Connected,
    
    /// <summary>
    /// 连接失败
    /// </summary>
    Failed,
    
    /// <summary>
    /// 已断开
    /// </summary>
    Disconnected
}
