using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 笔记服务接口
/// </summary>
public interface INoteService
{
    /// <summary>
    /// 获取用户的笔记列表
    /// </summary>
    Task<List<Note>> GetUserNotesAsync(string userId, int page = 1, int pageSize = 20, string? search = null, string? category = null, string? tag = null);

    /// <summary>
    /// 获取笔记总数
    /// </summary>
    Task<int> GetUserNotesCountAsync(string userId, string? search = null, string? category = null, string? tag = null);

    /// <summary>
    /// 根据ID获取笔记
    /// </summary>
    Task<Note?> GetNoteByIdAsync(int id, string userId);

    /// <summary>
    /// 创建笔记
    /// </summary>
    Task<Note> CreateNoteAsync(Note note);

    /// <summary>
    /// 更新笔记
    /// </summary>
    Task<Note> UpdateNoteAsync(Note note);

    /// <summary>
    /// 删除笔记
    /// </summary>
    Task<bool> DeleteNoteAsync(int id, string userId);

    /// <summary>
    /// 获取笔记的所有分类
    /// </summary>
    Task<List<string>> GetCategoriesAsync(string userId);

    /// <summary>
    /// 获取笔记的所有标签
    /// </summary>
    Task<List<string>> GetTagsAsync(string userId);

    /// <summary>
    /// 搜索笔记
    /// </summary>
    Task<List<Note>> SearchNotesAsync(string userId, string query);

    /// <summary>
    /// 导出笔记为HTML
    /// </summary>
    Task<string> ExportNoteAsHtmlAsync(int noteId, string userId);

    /// <summary>
    /// 导出笔记为Markdown
    /// </summary>
    Task<string> ExportNoteAsMarkdownAsync(int noteId, string userId);

    /// <summary>
    /// 批量导出笔记
    /// </summary>
    Task<byte[]> ExportNotesAsZipAsync(List<int> noteIds, string userId);
}
