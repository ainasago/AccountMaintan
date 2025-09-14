using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 网站账号实体
/// </summary>
[Table(Name = "WebsiteAccounts")]
public class WebsiteAccount
{
    /// <summary>
    /// 账号ID
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
    /// 账号类型
    /// </summary>
    [Column(StringLength = 50, IsNullable = false)]
    public string AccountType { get; set; } = "Admin";

    /// <summary>
    /// 用户名
    /// </summary>
    [Column(StringLength = 100, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码（加密存储）
    /// </summary>
    [Column(StringLength = 1000, IsNullable = true)]
    public string? Password { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [Column(StringLength = 255, IsNullable = true)]
    public string? Email { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? Notes { get; set; }

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
    /// 最后使用时间
    /// </summary>
    [Column(IsNullable = true)]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 关联的网站
    /// </summary>
    public virtual Website? Website { get; set; }
}

/// <summary>
/// 账号类型枚举
/// </summary>
public enum AccountType
{
    /// <summary>
    /// 管理员账号
    /// </summary>
    Admin,
    
    /// <summary>
    /// 数据库账号
    /// </summary>
    Database,
    
    /// <summary>
    /// FTP账号
    /// </summary>
    Ftp,
    
    /// <summary>
    /// SSH账号
    /// </summary>
    Ssh,
    
    /// <summary>
    /// 其他账号
    /// </summary>
    Other
}

