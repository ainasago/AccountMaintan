using System.Text;
using WebUI.Data;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 笔记服务实现
/// </summary>
public class NoteService : INoteService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NoteService> _logger;

    public NoteService(AppDbContext context, ILogger<NoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Note>> GetUserNotesAsync(string userId, int page = 1, int pageSize = 20, string? search = null, string? category = null, string? tag = null)
    {
        var query = _context.Fsql.Select<Note>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.IsPinned)
            .OrderByDescending(n => n.UpdatedAt);

        // 搜索过滤
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(n => n.Title.Contains(search) || 
                                   n.Content.Contains(search) || 
                                   n.Summary.Contains(search));
        }

        // 分类过滤
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(n => n.Category == category);
        }

        // 标签过滤
        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(n => n.Tags.Contains(tag));
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserNotesCountAsync(string userId, string? search = null, string? category = null, string? tag = null)
    {
        var query = _context.Fsql.Select<Note>().Where(n => n.UserId == userId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(n => n.Title.Contains(search) || 
                                   n.Content.Contains(search) || 
                                   n.Summary.Contains(search));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(n => n.Category == category);
        }

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(n => n.Tags.Contains(tag));
        }

        return (int)await query.CountAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(int id, string userId)
    {
        return await _context.Fsql.Select<Note>()
            .Where(n => n.Id == id && n.UserId == userId)
            .FirstAsync();
    }

    public async Task<Note> CreateNoteAsync(Note note)
    {
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        // 生成摘要
        if (string.IsNullOrEmpty(note.Summary))
        {
            note.Summary = GenerateSummary(note.Content);
        }

        var insertedId = await _context.Fsql.Insert(note).ExecuteIdentityAsync();
        note.Id = (int)insertedId;

        _logger.LogInformation("创建笔记: {NoteId}, 用户: {UserId}", note.Id, note.UserId);
        return note;
    }

    public async Task<Note> UpdateNoteAsync(Note note)
    {
        var existingNote = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == note.Id)
            .FirstAsync();
        
        if (existingNote == null)
        {
            throw new ArgumentException("笔记不存在");
        }

        existingNote.Title = note.Title;
        existingNote.Content = note.Content;
        existingNote.Summary = string.IsNullOrEmpty(note.Summary) ? GenerateSummary(note.Content) : note.Summary;
        existingNote.Tags = note.Tags;
        existingNote.Category = note.Category;
        existingNote.IsPinned = note.IsPinned;
        existingNote.IsPublic = note.IsPublic;
        existingNote.UpdatedAt = DateTime.UtcNow;

        await _context.Fsql.Update<Note>()
            .SetSource(existingNote)
            .ExecuteAffrowsAsync();

        _logger.LogInformation("更新笔记: {NoteId}, 用户: {UserId}", note.Id, note.UserId);
        return existingNote;
    }

    public async Task<bool> DeleteNoteAsync(int id, string userId)
    {
        var note = await _context.Fsql.Select<Note>()
            .Where(n => n.Id == id && n.UserId == userId)
            .FirstAsync();
        
        if (note == null)
        {
            return false;
        }

        await _context.Fsql.Delete<Note>()
            .Where(n => n.Id == id)
            .ExecuteAffrowsAsync();

        _logger.LogInformation("删除笔记: {NoteId}, 用户: {UserId}", id, userId);
        return true;
    }

    public async Task<List<string>> GetCategoriesAsync(string userId)
    {
        var categories = await _context.Fsql.Select<Note>()
            .Where(n => n.UserId == userId && !string.IsNullOrEmpty(n.Category))
            .Distinct()
            .ToListAsync(n => n.Category!);

        return categories.OrderBy(c => c).ToList();
    }

    public async Task<List<string>> GetTagsAsync(string userId)
    {
        var allTags = await _context.Fsql.Select<Note>()
            .Where(n => n.UserId == userId && !string.IsNullOrEmpty(n.Tags))
            .ToListAsync(n => n.Tags!);

        var tags = new HashSet<string>();
        foreach (var tagString in allTags)
        {
            var tagList = tagString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim())
                                  .Where(t => !string.IsNullOrEmpty(t));
            foreach (var tag in tagList)
            {
                tags.Add(tag);
            }
        }

        return tags.OrderBy(t => t).ToList();
    }

    public async Task<List<Note>> SearchNotesAsync(string userId, string query)
    {
        return await _context.Fsql.Select<Note>()
            .Where(n => n.UserId == userId && 
                       (n.Title.Contains(query) || 
                        n.Content.Contains(query) || 
                        n.Summary.Contains(query) ||
                        n.Tags.Contains(query)))
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task<string> ExportNoteAsHtmlAsync(int noteId, string userId)
    {
        var note = await GetNoteByIdAsync(noteId, userId);
        if (note == null)
        {
            throw new ArgumentException("笔记不存在");
        }

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"zh-CN\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine($"<title>{note.Title}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }");
        html.AppendLine("h1 { color: #333; border-bottom: 2px solid #eee; padding-bottom: 10px; }");
        html.AppendLine(".meta { color: #666; font-size: 14px; margin-bottom: 20px; }");
        html.AppendLine(".content { line-height: 1.6; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"<h1>{note.Title}</h1>");
        html.AppendLine("<div class=\"meta\">");
        html.AppendLine($"<p>创建时间: {note.CreatedAt:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine($"<p>更新时间: {note.UpdatedAt:yyyy-MM-dd HH:mm:ss}</p>");
        if (!string.IsNullOrEmpty(note.Category))
        {
            html.AppendLine($"<p>分类: {note.Category}</p>");
        }
        if (!string.IsNullOrEmpty(note.Tags))
        {
            html.AppendLine($"<p>标签: {note.Tags}</p>");
        }
        html.AppendLine("</div>");
        html.AppendLine($"<div class=\"content\">{note.Content}</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> ExportNoteAsMarkdownAsync(int noteId, string userId)
    {
        var note = await GetNoteByIdAsync(noteId, userId);
        if (note == null)
        {
            throw new ArgumentException("笔记不存在");
        }

        var markdown = new StringBuilder();
        markdown.AppendLine($"# {note.Title}");
        markdown.AppendLine();
        markdown.AppendLine($"**创建时间:** {note.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        markdown.AppendLine($"**更新时间:** {note.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        if (!string.IsNullOrEmpty(note.Category))
        {
            markdown.AppendLine($"**分类:** {note.Category}");
        }
        if (!string.IsNullOrEmpty(note.Tags))
        {
            markdown.AppendLine($"**标签:** {note.Tags}");
        }
        markdown.AppendLine();
        markdown.AppendLine("---");
        markdown.AppendLine();
        markdown.AppendLine(HtmlToMarkdown(note.Content));

        return markdown.ToString();
    }

    public async Task<byte[]> ExportNotesAsZipAsync(List<int> noteIds, string userId)
    {
        // 这里需要实现ZIP导出功能
        // 暂时返回空数组，后续实现
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    private string GenerateSummary(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // 移除HTML标签
        var plainText = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", string.Empty);
        
        // 移除多余空白
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();
        
        // 截取前200个字符
        return plainText.Length > 200 ? plainText.Substring(0, 200) + "..." : plainText;
    }

    private string HtmlToMarkdown(string html)
    {
        // 简单的HTML到Markdown转换
        var markdown = html;
        
        // 转换标题
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<h1[^>]*>(.*?)</h1>", "# $1\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<h2[^>]*>(.*?)</h2>", "## $1\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<h3[^>]*>(.*?)</h3>", "### $1\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // 转换粗体和斜体
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<strong[^>]*>(.*?)</strong>", "**$1**", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<b[^>]*>(.*?)</b>", "**$1**", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<em[^>]*>(.*?)</em>", "*$1*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<i[^>]*>(.*?)</i>", "*$1*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // 转换段落
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<p[^>]*>(.*?)</p>", "$1\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // 转换换行
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<br[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // 移除其他HTML标签
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"<[^>]+>", string.Empty);
        
        return markdown.Trim();
    }
}