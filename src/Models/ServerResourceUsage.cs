using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 服务器资源使用情况实体
/// </summary>
[Table(Name = "ServerResourceUsages")]
public class ServerResourceUsage
{
    /// <summary>
    /// 记录ID
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
    /// 记录时间
    /// </summary>
    [Column(IsNullable = false)]
    public DateTime RecordTime { get; set; } = DateTime.Now;

    /// <summary>
    /// CPU使用率（百分比）
    /// </summary>
    [Column(IsNullable = true)]
    public double? CpuUsage { get; set; }

    /// <summary>
    /// 内存使用率（百分比）
    /// </summary>
    [Column(IsNullable = true)]
    public double? MemoryUsage { get; set; }

    /// <summary>
    /// 磁盘使用率（百分比）
    /// </summary>
    [Column(IsNullable = true)]
    public double? DiskUsage { get; set; }

    /// <summary>
    /// 网络入流量（字节）
    /// </summary>
    [Column(IsNullable = true)]
    public long? NetworkInBytes { get; set; }

    /// <summary>
    /// 网络出流量（字节）
    /// </summary>
    [Column(IsNullable = true)]
    public long? NetworkOutBytes { get; set; }

    /// <summary>
    /// 负载平均值
    /// </summary>
    [Column(IsNullable = true)]
    public double? LoadAverage { get; set; }

    /// <summary>
    /// 运行时间（秒）
    /// </summary>
    [Column(IsNullable = true)]
    public long? Uptime { get; set; }

    /// <summary>
    /// 进程数量
    /// </summary>
    [Column(IsNullable = true)]
    public int? ProcessCount { get; set; }

    /// <summary>
    /// 关联的服务器
    /// </summary>
    public virtual Server? Server { get; set; }
}

