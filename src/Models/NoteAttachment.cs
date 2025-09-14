using System.ComponentModel.DataAnnotations;
using FreeSql.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 笔记附件实体
/// </summary>
public class NoteAttachment
{
    /// <summary>
    /// 附件ID
    /// </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 笔记ID
    /// </summary>
    public int NoteId { get; set; }

    /// <summary>
    /// 笔记
    /// </summary>
    public virtual Note Note { get; set; } = null!;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文件名
    /// </summary>
    [Required]
    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public AttachmentType FileType { get; set; }

    /// <summary>
    /// 文件哈希（用于去重）
    /// </summary>
    [StringLength(64)]
    public string? FileHash { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 获取文件扩展名
    /// </summary>
    public string GetFileExtension()
    {
        return Path.GetExtension(OriginalFileName).ToLowerInvariant();
    }

    /// <summary>
    /// 是否为图片文件
    /// </summary>
    public bool IsImage()
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
        return imageExtensions.Contains(GetFileExtension());
    }

    /// <summary>
    /// 是否为音频文件
    /// </summary>
    public bool IsAudio()
    {
        var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a", ".flac" };
        return audioExtensions.Contains(GetFileExtension());
    }

    /// <summary>
    /// 是否为视频文件
    /// </summary>
    public bool IsVideo()
    {
        var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
        return videoExtensions.Contains(GetFileExtension());
    }

    /// <summary>
    /// 获取格式化的文件大小
    /// </summary>
    public string GetFormattedFileSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 附件类型枚举
/// </summary>
public enum AttachmentType
{
    /// <summary>
    /// 图片
    /// </summary>
    Image = 1,

    /// <summary>
    /// 音频
    /// </summary>
    Audio = 2,

    /// <summary>
    /// 视频
    /// </summary>
    Video = 3,

    /// <summary>
    /// 文档
    /// </summary>
    Document = 4,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 5
}
