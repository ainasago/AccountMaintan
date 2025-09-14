using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// 笔记管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(
        INoteService noteService,
        IFileUploadService fileUploadService,
        ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// 获取笔记列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? tag = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notes = await _noteService.GetUserNotesAsync(userId, page, pageSize, search, category, tag);
            var totalCount = await _noteService.GetUserNotesCountAsync(userId, search, category, tag);

            return Ok(new
            {
                success = true,
                data = notes,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取笔记列表失败");
            return StatusCode(500, new { success = false, message = "获取笔记列表失败" });
        }
    }

    /// <summary>
    /// 获取指定笔记的附件列表
    /// </summary>
    [HttpGet("{id}/attachments")]
    public async Task<IActionResult> GetNoteAttachments(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = await _noteService.GetNoteByIdAsync(id, userId);
            if (note == null)
            {
                return NotFound(new { success = false, message = "笔记不存在" });
            }

            var attachments = await _fileUploadService.GetAttachmentsByNoteIdAsync(id, userId);
            return Ok(new { success = true, data = attachments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取附件列表失败");
            return StatusCode(500, new { success = false, message = "获取附件列表失败" });
        }
    }

    /// <summary>
    /// 获取笔记详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNote(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = await _noteService.GetNoteByIdAsync(id, userId);

            if (note == null)
            {
                return NotFound(new { success = false, message = "笔记不存在" });
            }

            return Ok(new { success = true, data = note });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取笔记详情失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "获取笔记详情失败" });
        }
    }

    /// <summary>
    /// 创建笔记
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                Summary = request.Summary,
                Tags = request.Tags,
                Category = request.Category,
                IsPinned = request.IsPinned,
                IsPublic = request.IsPublic,
                UserId = userId
            };

            var createdNote = await _noteService.CreateNoteAsync(note);

            return CreatedAtAction(nameof(GetNote), new { id = createdNote.Id }, new
            {
                success = true,
                data = createdNote,
                message = "笔记创建成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建笔记失败");
            return StatusCode(500, new { success = false, message = "创建笔记失败" });
        }
    }

    /// <summary>
    /// 更新笔记
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = await _noteService.GetNoteByIdAsync(id, userId);

            if (note == null)
            {
                return NotFound(new { success = false, message = "笔记不存在" });
            }

            note.Title = request.Title;
            note.Content = request.Content;
            note.Summary = request.Summary;
            note.Tags = request.Tags;
            note.Category = request.Category;
            note.IsPinned = request.IsPinned;
            note.IsPublic = request.IsPublic;
            note.Id = id; // 确保ID被设置

            var updatedNote = await _noteService.UpdateNoteAsync(note);

            return Ok(new
            {
                success = true,
                data = updatedNote,
                message = "笔记更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新笔记失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "更新笔记失败" });
        }
    }

    /// <summary>
    /// 删除笔记
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _noteService.DeleteNoteAsync(id, userId);

            if (!success)
            {
                return NotFound(new { success = false, message = "笔记不存在" });
            }

            return Ok(new { success = true, message = "笔记删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除笔记失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "删除笔记失败" });
        }
    }

    /// <summary>
    /// 获取分类列表
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var userId = GetCurrentUserId();
            var categories = await _noteService.GetCategoriesAsync(userId);

            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类列表失败");
            return StatusCode(500, new { success = false, message = "获取分类列表失败" });
        }
    }

    /// <summary>
    /// 获取标签列表
    /// </summary>
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        try
        {
            var userId = GetCurrentUserId();
            var tags = await _noteService.GetTagsAsync(userId);

            return Ok(new { success = true, data = tags });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签列表失败");
            return StatusCode(500, new { success = false, message = "获取标签列表失败" });
        }
    }

    /// <summary>
    /// 搜索笔记
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchNotes([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest(new { success = false, message = "搜索关键词不能为空" });
            }

            var userId = GetCurrentUserId();
            var notes = await _noteService.SearchNotesAsync(userId, query);

            return Ok(new { success = true, data = notes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索笔记失败: {Query}", query);
            return StatusCode(500, new { success = false, message = "搜索笔记失败" });
        }
    }

    /// <summary>
    /// 导出笔记为HTML
    /// </summary>
    [HttpGet("{id}/export/html")]
    public async Task<IActionResult> ExportAsHtml(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var html = await _noteService.ExportNoteAsHtmlAsync(id, userId);

            return Content(html, "text/html");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出HTML失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "导出HTML失败" });
        }
    }

    /// <summary>
    /// 导出笔记为Markdown
    /// </summary>
    [HttpGet("{id}/export/markdown")]
    public async Task<IActionResult> ExportAsMarkdown(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var markdown = await _noteService.ExportNoteAsMarkdownAsync(id, userId);

            return Content(markdown, "text/markdown");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出Markdown失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "导出Markdown失败" });
        }
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> UploadFile(int id, IFormFile file)
    {
        try
        {
            var userId = GetCurrentUserId();
            var attachment = await _fileUploadService.UploadFileAsync(file, id, userId);

            return Ok(new
            {
                success = true,
                data = attachment,
                message = "文件上传成功"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "上传文件失败" });
        }
    }

    /// <summary>
    /// 批量上传文件
    /// </summary>
    [HttpPost("{id}/attachments/batch")]
    public async Task<IActionResult> UploadFiles(int id, List<IFormFile> files)
    {
        try
        {
            var userId = GetCurrentUserId();
            var attachments = await _fileUploadService.UploadFilesAsync(files, id, userId);

            return Ok(new
            {
                success = true,
                data = attachments,
                message = $"成功上传 {attachments.Count} 个文件"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量上传文件失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "批量上传文件失败" });
        }
    }

    /// <summary>
    /// 从剪切板上传图片
    /// </summary>
    [HttpPost("{id}/attachments/clipboard")]
    public async Task<IActionResult> UploadFromClipboard(int id, [FromBody] ClipboardUploadRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var imageData = Convert.FromBase64String(request.ImageData);
            var attachment = await _fileUploadService.UploadImageFromClipboardAsync(imageData, request.FileName, id, userId);

            if (attachment == null)
            {
                return BadRequest(new { success = false, message = "无效的图片数据" });
            }

            return Ok(new
            {
                success = true,
                data = attachment,
                message = "图片上传成功"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从剪切板上传图片失败: {NoteId}", id);
            return StatusCode(500, new { success = false, message = "从剪切板上传图片失败" });
        }
    }

    /// <summary>
    /// 删除附件
    /// </summary>
    [HttpDelete("attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _fileUploadService.DeleteFileAsync(attachmentId, userId);

            if (!success)
            {
                return NotFound(new { success = false, message = "附件不存在" });
            }

            return Ok(new { success = true, message = "附件删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除附件失败: {AttachmentId}", attachmentId);
            return StatusCode(500, new { success = false, message = "删除附件失败" });
        }
    }

    /// <summary>
    /// 获取附件
    /// </summary>
    [HttpGet("attachments/{attachmentId}")]
    public async Task<IActionResult> GetAttachment(int attachmentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var attachment = await _fileUploadService.GetFileAsync(attachmentId, userId);

            if (attachment == null)
            {
                return NotFound(new { success = false, message = "附件不存在" });
            }

            return Ok(new { success = true, data = attachment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取附件失败: {AttachmentId}", attachmentId);
            return StatusCode(500, new { success = false, message = "获取附件失败" });
        }
    }

    /// <summary>
    /// 下载附件
    /// </summary>
    [HttpGet("attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var attachment = await _fileUploadService.GetFileAsync(attachmentId, userId);

            if (attachment == null)
            {
                return NotFound(new { success = false, message = "附件不存在" });
            }

            var stream = await _fileUploadService.GetFileStreamAsync(attachmentId, userId);
            if (stream == null)
            {
                return NotFound(new { success = false, message = "文件不存在" });
            }

            return File(stream, attachment.MimeType, attachment.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载附件失败: {AttachmentId}", attachmentId);
            return StatusCode(500, new { success = false, message = "下载附件失败" });
        }
    }

    /// <summary>
    /// 获取附件缩略图
    /// </summary>
    [HttpGet("attachments/{attachmentId}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int attachmentId, [FromQuery] int width = 200, [FromQuery] int height = 200)
    {
        try
        {
            var userId = GetCurrentUserId();
            var stream = await _fileUploadService.GetThumbnailAsync(attachmentId, userId, width, height);

            if (stream == null)
            {
                return NotFound(new { success = false, message = "缩略图不存在" });
            }

            return File(stream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缩略图失败: {AttachmentId}", attachmentId);
            return StatusCode(500, new { success = false, message = "获取缩略图失败" });
        }
    }

    /// <summary>
    /// 读取文本附件
    /// </summary>
    [HttpGet("attachments/{attachmentId}/text")]
    public async Task<IActionResult> ReadAttachmentText(int attachmentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var text = await _fileUploadService.ReadAttachmentTextAsync(attachmentId, userId);
            if (text == null)
            {
                return BadRequest(new { success = false, message = "不支持的文本类型或文件不存在" });
            }
            return Ok(new { success = true, data = text });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文本附件失败: {AttachmentId}", attachmentId);
            return StatusCode(500, new { success = false, message = "读取文本附件失败" });
        }
    }

    /// <summary>
    /// 更新文本附件
    /// </summary>
    [HttpPut("attachments/{attachmentId}/text")]
    public async Task<IActionResult> UpdateAttachmentText(int attachmentId, [FromBody] string content)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ok = await _fileUploadService.UpdateAttachmentTextAsync(attachmentId, content, userId);
            if (!ok)
            {
                return BadRequest(new { success = false, message = "不支持的文本类型或文件不存在" });
            }
            return Ok(new { success = true, message = "保存成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新文本附件失败: {AttachmentId}", attachmentId);
            return StatusCode(500, new { success = false, message = "更新文本附件失败" });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("用户未认证");
    }
}

/// <summary>
/// 创建笔记请求
/// </summary>
public class CreateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public string? Category { get; set; }
    public bool IsPinned { get; set; }
    public bool IsPublic { get; set; }
}

/// <summary>
/// 更新笔记请求
/// </summary>
public class UpdateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public string? Category { get; set; }
    public bool IsPinned { get; set; }
    public bool IsPublic { get; set; }
}

/// <summary>
/// 剪切板上传请求
/// </summary>
public class ClipboardUploadRequest
{
    public string ImageData { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
