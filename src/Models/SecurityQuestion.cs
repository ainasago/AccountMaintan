using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 安全问题实体
/// </summary>
public class SecurityQuestion
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
    /// 问题内容
    /// </summary>
    [StringLength(200, ErrorMessage = "安全问题长度不能超过200个字符")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 答案（加密存储）
    /// </summary>
    [StringLength(200, ErrorMessage = "安全问题答案长度不能超过200个字符")]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 关联的账号
    /// </summary>
    public virtual Account Account { get; set; } = null!;
}
