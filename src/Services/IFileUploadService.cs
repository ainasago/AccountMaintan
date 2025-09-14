using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 文件上传服务接口
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    Task<NoteAttachment> UploadFileAsync(IFormFile file, int noteId, string userId);

    /// <summary>
    /// 上传多个文件
    /// </summary>
    Task<List<NoteAttachment>> UploadFilesAsync(List<IFormFile> files, int noteId, string userId);

    /// <summary>
    /// 从剪切板上传图片
    /// </summary>
    Task<NoteAttachment?> UploadImageFromClipboardAsync(byte[] imageData, string fileName, int noteId, string userId);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteFileAsync(int attachmentId, string userId);

    /// <summary>
    /// 获取文件
    /// </summary>
    Task<NoteAttachment?> GetFileAsync(int attachmentId, string userId);

    /// <summary>
    /// 获取文件流
    /// </summary>
    Task<Stream?> GetFileStreamAsync(int attachmentId, string userId);

    /// <summary>
    /// 获取文件的缩略图
    /// </summary>
    Task<Stream?> GetThumbnailAsync(int attachmentId, string userId, int width = 200, int height = 200);

    /// <summary>
    /// 验证文件类型
    /// </summary>
    bool IsValidFileType(string fileName, string mimeType);

    /// <summary>
    /// 获取文件类型
    /// </summary>
    AttachmentType GetFileType(string fileName, string mimeType);

    /// <summary>
    /// 生成文件哈希
    /// </summary>
    Task<string> GenerateFileHashAsync(Stream fileStream);

    /// <summary>
    /// 清理未使用的文件
    /// </summary>
    Task<int> CleanupUnusedFilesAsync();

    /// <summary>
    /// 根据笔记ID获取附件列表
    /// </summary>
    Task<List<NoteAttachment>> GetAttachmentsByNoteIdAsync(int noteId, string userId);

    /// <summary>
    /// 读取文本附件内容（仅限文本/可读扩展名）
    /// </summary>
    Task<string?> ReadAttachmentTextAsync(int attachmentId, string userId);

    /// <summary>
    /// 更新文本附件内容（仅限文本/可写扩展名）
    /// </summary>
    Task<bool> UpdateAttachmentTextAsync(int attachmentId, string content, string userId);
}
