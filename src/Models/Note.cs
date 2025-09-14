using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 笔记实体
/// </summary>
public class Note
{
    /// <summary>
    /// 笔记ID
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 笔记标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 笔记内容（HTML格式）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 笔记摘要
    /// </summary>
    [StringLength(500)]
    public string? Summary { get; set; }

    /// <summary>
    /// 标签（逗号分隔）
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// 附件列表
    /// </summary>
    public virtual ICollection<NoteAttachment> Attachments { get; set; } = new List<NoteAttachment>();

    /// <summary>
    /// 获取标签列表
    /// </summary>
    public List<string> GetTagsList()
    {
        if (string.IsNullOrEmpty(Tags))
            return new List<string>();
        
        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(t => t.Trim())
                  .Where(t => !string.IsNullOrEmpty(t))
                  .ToList();
    }

    /// <summary>
    /// 设置标签列表
    /// </summary>
    public void SetTagsList(List<string> tags)
    {
        Tags = string.Join(", ", tags.Where(t => !string.IsNullOrWhiteSpace(t)));
    }
}
