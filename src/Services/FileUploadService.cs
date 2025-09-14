using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography;
using WebUI.Data;
using WebUI.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WebUI.Services;

/// <summary>
/// 文件上传服务实现
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadPath;

    // 旧的类型/大小限制移除：不再限制上传类型与大小，按原样接收

    public FileUploadService(AppDbContext context, ILogger<FileUploadService> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
        _uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "notes");
        
        // 确保上传目录存在
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<NoteAttachment> UploadFileAsync(IFormFile file, int noteId, string userId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("文件不能为空");
        }

        // 不限制文件大小与类型

        // 验证笔记是否存在且属于当前用户
        var note = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == noteId && n.UserId == userId)
            .FirstAsync();
        if (note == null)
        {
            throw new ArgumentException("笔记不存在或无权限");
        }

        // 生成唯一文件名
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_uploadPath, storedFileName);

        // 保存文件
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 生成文件哈希
        var fileHash = await GenerateFileHashAsync(new FileStream(filePath, FileMode.Open, FileAccess.Read));

        // 创建附件记录
        var attachment = new NoteAttachment
        {
            NoteId = noteId,
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            FileSize = file.Length,
            MimeType = file.ContentType,
            FileType = GetFileType(file.FileName, file.ContentType),
            FileHash = fileHash,
            UploadedAt = DateTime.UtcNow
        };

        var attachmentId = await _context.Fsql.Insert(attachment).ExecuteIdentityAsync();
        attachment.Id = (int)attachmentId;

        _logger.LogInformation("上传文件: {FileName}, 笔记: {NoteId}, 用户: {UserId}", file.FileName, noteId, userId);

        return attachment;
    }

    public async Task<List<NoteAttachment>> UploadFilesAsync(List<IFormFile> files, int noteId, string userId)
    {
        var attachments = new List<NoteAttachment>();
        
        foreach (var file in files)
        {
            try
            {
                var attachment = await UploadFileAsync(file, noteId, userId);
                attachments.Add(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传文件失败: {FileName}", file.FileName);
                // 继续处理其他文件
            }
        }

        return attachments;
    }

    public async Task<NoteAttachment?> UploadImageFromClipboardAsync(byte[] imageData, string fileName, int noteId, string userId)
    {
        if (imageData == null || imageData.Length == 0)
        {
            return null;
        }

        // 验证笔记是否存在且属于当前用户
        var note = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == noteId && n.UserId == userId)
            .FirstAsync();
        if (note == null)
        {
            throw new ArgumentException("笔记不存在或无权限");
        }

        // 生成唯一文件名
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileExtension))
        {
            fileExtension = ".png"; // 默认PNG格式
        }
        
        var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_uploadPath, storedFileName);

        // 保存文件
        await File.WriteAllBytesAsync(filePath, imageData);

        // 生成文件哈希
        var fileHash = await GenerateFileHashAsync(new MemoryStream(imageData));

        // 创建附件记录
        var attachment = new NoteAttachment
        {
            NoteId = noteId,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            FileSize = imageData.Length,
            MimeType = "image/png", // 默认MIME类型
            FileType = AttachmentType.Image,
            FileHash = fileHash,
            UploadedAt = DateTime.UtcNow
        };

        var attachmentId = await _context.Fsql.Insert(attachment).ExecuteIdentityAsync();
        attachment.Id = (int)attachmentId;

        _logger.LogInformation("从剪切板上传图片: {FileName}, 笔记: {NoteId}, 用户: {UserId}", fileName, noteId, userId);

        return attachment;
    }

    public async Task<bool> DeleteFileAsync(int attachmentId, string userId)
    {
        var attachment = await _context.Fsql.Select<NoteAttachment>()
            .Where(a => a.Id == attachmentId)
            .FirstAsync();

        if (attachment == null)
        {
            return false;
        }

        // 验证笔记是否属于当前用户
        var note = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == attachment.NoteId && n.UserId == userId)
            .FirstAsync();

        if (note == null)
        {
            return false;
        }

        // 删除物理文件
        if (File.Exists(attachment.FilePath))
        {
            try
            {
                File.Delete(attachment.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除物理文件失败: {FilePath}", attachment.FilePath);
            }
        }

        // 从数据库删除记录
        await _context.Fsql.Delete<NoteAttachment>()
            .Where(a => a.Id == attachmentId)
            .ExecuteAffrowsAsync();

        _logger.LogInformation("删除文件: {AttachmentId}, 用户: {UserId}", attachmentId, userId);
        return true;
    }

    public async Task<NoteAttachment?> GetFileAsync(int attachmentId, string userId)
    {
        var attachment = await _context.Fsql.Select<NoteAttachment>()
            .Where(a => a.Id == attachmentId)
            .FirstAsync();

        if (attachment == null)
        {
            return null;
        }

        // 验证笔记是否属于当前用户
        var note = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == attachment.NoteId && n.UserId == userId)
            .FirstAsync();

        return note != null ? attachment : null;
    }

    public async Task<Stream?> GetFileStreamAsync(int attachmentId, string userId)
    {
        var attachment = await GetFileAsync(attachmentId, userId);
        if (attachment == null || !File.Exists(attachment.FilePath))
        {
            return null;
        }

        return new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<Stream?> GetThumbnailAsync(int attachmentId, string userId, int width = 200, int height = 200)
    {
        var attachment = await GetFileAsync(attachmentId, userId);
        if (attachment == null || !attachment.IsImage() || !File.Exists(attachment.FilePath))
        {
            return null;
        }

        try
        {
            using var image = await Image.LoadAsync(attachment.FilePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            var thumbnailStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Position = 0;

            return thumbnailStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成缩略图失败: {AttachmentId}", attachmentId);
            return null;
        }
    }

    public bool IsValidFileType(string fileName, string mimeType)
    {
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // 不再限制，始终返回 true
        return true;
    }

    public AttachmentType GetFileType(string fileName, string mimeType)
    {
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

        if ((mimeType?.StartsWith("image/") ?? false) || 
            new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" }.Contains(fileExtension))
        {
            return AttachmentType.Image;
        }

        if ((mimeType?.StartsWith("audio/") ?? false) || 
            new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a", ".flac" }.Contains(fileExtension))
        {
            return AttachmentType.Audio;
        }

        if ((mimeType?.StartsWith("video/") ?? false) || 
            new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" }.Contains(fileExtension))
        {
            return AttachmentType.Video;
        }

        if (new[] { ".pdf", ".doc", ".docx", ".txt", ".md", ".json", ".xml", ".csv", ".log", ".html", ".css", ".js" }.Contains(fileExtension) || (mimeType?.StartsWith("text/") ?? false))
        {
            return AttachmentType.Document;
        }

        return AttachmentType.Other;
    }

    public async Task<string> GenerateFileHashAsync(Stream fileStream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<int> CleanupUnusedFilesAsync()
    {
        // 查找已删除的附件
        var deletedAttachments = await _context.Fsql.Select<NoteAttachment>()
            .Where(a => a.IsDeleted)
            .ToListAsync();

        var cleanedCount = 0;
        foreach (var attachment in deletedAttachments)
        {
            if (File.Exists(attachment.FilePath))
            {
                try
                {
                    File.Delete(attachment.FilePath);
                    cleanedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理文件失败: {FilePath}", attachment.FilePath);
                }
            }
        }

        // 从数据库中删除记录
        await _context.Fsql.Delete<NoteAttachment>()
            .Where(a => a.IsDeleted)
            .ExecuteAffrowsAsync();

        _logger.LogInformation("清理了 {Count} 个未使用的文件", cleanedCount);
        return cleanedCount;
    }

    public async Task<List<NoteAttachment>> GetAttachmentsByNoteIdAsync(int noteId, string userId)
    {
        // 确保笔记属于用户
        var note = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == noteId && n.UserId == userId)
            .FirstAsync();
        if (note == null)
        {
            return new List<NoteAttachment>();
        }

        return await _context.Fsql.Select<NoteAttachment>()
            .Where(a => a.NoteId == noteId && !a.IsDeleted)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task<string?> ReadAttachmentTextAsync(int attachmentId, string userId)
    {
        var attachment = await GetFileAsync(attachmentId, userId);
        if (attachment == null) return null;
        if (!File.Exists(attachment.FilePath)) return null;

        // 尝试以UTF8读取，失败则返回null，保证不崩溃
        try
        {
            return await File.ReadAllTextAsync(attachment.FilePath);
        }
        catch
        {
            try
            {
                // 再尝试以默认编码读取，仍失败则返回null
                return await File.ReadAllTextAsync(attachment.FilePath, System.Text.Encoding.Default);
            }
            catch
            {
                return null;
            }
        }
    }

    public async Task<bool> UpdateAttachmentTextAsync(int attachmentId, string content, string userId)
    {
        var attachment = await GetFileAsync(attachmentId, userId);
        if (attachment == null) return false;
        if (!File.Exists(attachment.FilePath)) return false;

        await File.WriteAllTextAsync(attachment.FilePath, content);
        return true;
    }
}