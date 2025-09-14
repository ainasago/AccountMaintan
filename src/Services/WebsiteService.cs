using FreeSql;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Services;

/// <summary>
/// 网站管理服务实现
/// </summary>
public class WebsiteService : IWebsiteService
{
    private readonly IFreeSql _freeSql;
    private readonly ISupervisorService _supervisorService;
    private readonly ISshService _sshService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<WebsiteService> _logger;

    public WebsiteService(
        IFreeSql freeSql,
        ISupervisorService supervisorService,
        ISshService sshService,
        IEncryptionService encryptionService,
        ILogger<WebsiteService> logger)
    {
        _freeSql = freeSql;
        _supervisorService = supervisorService;
        _sshService = sshService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<List<Website>> GetWebsitesByUserIdAsync(string userId)
    {
        try
        {
            return await _freeSql.Select<Website>()
                .Where(w => w.UserId == userId)
                .Include(w => w.Server)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户网站列表失败: {UserId}", userId);
            return new List<Website>();
        }
    }

    public async Task<Website?> GetWebsiteByIdAsync(Guid id, string userId)
    {
        try
        {
            return await _freeSql.Select<Website>()
                .Where(w => w.Id == id && w.UserId == userId)
                .Include(w => w.Server)
                .IncludeMany(w => w.WebsiteAccounts)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站信息失败: {WebsiteId}", id);
            return null;
        }
    }

    public async Task<bool> CreateWebsiteAsync(Website website)
    {
        try
        {
            var result = await _freeSql.Insert(website).ExecuteAffrowsAsync();
            if (result > 0)
            {
                _logger.LogInformation("网站创建成功: {WebsiteName}", website.Name);
                return true;
            }
            else
            {
                _logger.LogWarning("网站创建失败，影响行数为0: {WebsiteName}", website.Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建网站失败: {WebsiteName}", website.Name);
            return false;
        }
    }

    public async Task<bool> UpdateWebsiteAsync(Website website)
    {
        try
        {
            var result = await _freeSql.Update<Website>()
                .SetSource(website)
                .Where(w => w.Id == website.Id && w.UserId == website.UserId)
                .ExecuteAffrowsAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("网站更新成功: {WebsiteName}", website.Name);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站失败: {WebsiteName}", website.Name);
            return false;
        }
    }

    public async Task<bool> DeleteWebsiteAsync(Guid id, string userId)
    {
        try
        {
            // 先删除相关的账号和日志
            await _freeSql.Delete<WebsiteAccount>().Where(wa => wa.WebsiteId == id).ExecuteAffrowsAsync();
            await _freeSql.Delete<WebsiteAccessLog>().Where(wal => wal.WebsiteId == id).ExecuteAffrowsAsync();
            
            // 删除网站
            var result = await _freeSql.Delete<Website>()
                .Where(w => w.Id == id && w.UserId == userId)
                .ExecuteAffrowsAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("网站删除成功: {WebsiteId}", id);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除网站失败: {WebsiteId}", id);
            return false;
        }
    }

    public async Task<string> CheckWebsiteStatusAsync(Website website)
    {
        try
        {
            if (website.Server == null)
            {
                return "Unknown";
            }

            // 检查网站可访问性
            var isAccessible = await _sshService.CheckWebsiteAccessibilityAsync(website);
            if (isAccessible)
            {
                return "Running";
            }

            // 如果配置了Supervisor进程名，检查进程状态
            if (!string.IsNullOrEmpty(website.SupervisorProcessName))
            {
                var processInfo = await _supervisorService.GetProcessInfoAsync(website.Server, website.SupervisorProcessName);
                if (processInfo != null)
                {
                    return processInfo.Status;
                }
            }

            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查网站状态失败: {WebsiteName}", website.Name);
            return "Error";
        }
    }

    public async Task<bool> RestartWebsiteAsync(Website website)
    {
        try
        {
            if (website.Server == null || string.IsNullOrEmpty(website.SupervisorProcessName))
            {
                return false;
            }

            var result = await _supervisorService.RestartProcessAsync(website.Server, website.SupervisorProcessName);
            
            // 记录访问日志
            await LogWebsiteAccessAsync(website.Id, website.UserId, "Restart", 
                notes: $"通过Supervisor重启进程: {website.SupervisorProcessName}");
            
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启网站失败: {WebsiteName}", website.Name);
            return false;
        }
    }

    public async Task<string> GetWebsiteLogAsync(Website website, int lines = 100)
    {
        try
        {
            if (website.Server == null)
            {
                return "无法获取日志：服务器信息缺失";
            }

            // 如果有Supervisor进程名，获取Supervisor日志
            if (!string.IsNullOrEmpty(website.SupervisorProcessName))
            {
                return await _supervisorService.GetProcessLogAsync(website.Server, website.SupervisorProcessName, "stdout", lines);
            }

            // 否则尝试获取网站访问日志
            var logPath = $"/var/log/nginx/access.log"; // 默认Nginx日志路径
            return await _sshService.GetWebsiteLogAsync(website.Server, logPath, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站日志失败: {WebsiteName}", website.Name);
            return $"获取日志失败: {ex.Message}";
        }
    }

    public async Task<bool> LogWebsiteAccessAsync(Guid websiteId, string userId, string accessType, 
        string? ipAddress = null, string? userAgent = null, string? notes = null)
    {
        try
        {
            var log = new WebsiteAccessLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WebsiteId = websiteId,
                AccessType = accessType,
                AccessTime = DateTime.Now,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Notes = notes
            };

            await _freeSql.Insert(log).ExecuteAffrowsAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录网站访问日志失败: {WebsiteId}", websiteId);
            return false;
        }
    }

    public async Task<List<WebsiteAccount>> GetWebsiteAccountsAsync(Guid websiteId, string userId)
    {
        try
        {
            return await _freeSql.Select<WebsiteAccount>()
                .Where(wa => wa.WebsiteId == websiteId && wa.UserId == userId)
                .OrderBy(wa => wa.AccountType)
                .OrderBy(wa => wa.Username)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站账号列表失败: {WebsiteId}", websiteId);
            return new List<WebsiteAccount>();
        }
    }

    public async Task<bool> CreateWebsiteAccountAsync(WebsiteAccount account)
    {
        try
        {
            // 加密密码
            if (!string.IsNullOrEmpty(account.Password))
            {
                account.Password = _encryptionService.Encrypt(account.Password);
            }

            await _freeSql.Insert(account).ExecuteAffrowsAsync();
            _logger.LogInformation("网站账号创建成功: {Username}", account.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建网站账号失败: {Username}", account.Username);
            return false;
        }
    }

    public async Task<bool> UpdateWebsiteAccountAsync(WebsiteAccount account)
    {
        try
        {
            // 加密密码
            if (!string.IsNullOrEmpty(account.Password))
            {
                account.Password = _encryptionService.Encrypt(account.Password);
            }

            var result = await _freeSql.Update<WebsiteAccount>()
                .SetSource(account)
                .Where(wa => wa.Id == account.Id && wa.UserId == account.UserId)
                .ExecuteAffrowsAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("网站账号更新成功: {Username}", account.Username);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站账号失败: {Username}", account.Username);
            return false;
        }
    }

    public async Task<bool> DeleteWebsiteAccountAsync(Guid id, string userId)
    {
        try
        {
            var result = await _freeSql.Delete<WebsiteAccount>()
                .Where(wa => wa.Id == id && wa.UserId == userId)
                .ExecuteAffrowsAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("网站账号删除成功: {AccountId}", id);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除网站账号失败: {AccountId}", id);
            return false;
        }
    }

    public async Task<Dictionary<Guid, string>> BatchCheckWebsiteStatusAsync(string userId)
    {
        try
        {
            var websites = await GetWebsitesByUserIdAsync(userId);
            var statusDict = new Dictionary<Guid, string>();

            foreach (var website in websites)
            {
                var status = await CheckWebsiteStatusAsync(website);
                statusDict[website.Id] = status;
            }

            return statusDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量检查网站状态失败: {UserId}", userId);
            return new Dictionary<Guid, string>();
        }
    }

    /// <summary>
    /// 获取用户的所有网站分类
    /// </summary>
    public async Task<List<string>> GetUserCategoriesAsync(string userId)
    {
        try
        {
            var categories = await _freeSql.Select<Website>()
                .Where(w => w.UserId == userId && w.Category != null)
                .GroupBy(w => w.Category)
                .ToListAsync(g => g.Key!);
            
            return categories.OrderBy(c => c).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户网站分类失败: {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取用户的所有网站标签
    /// </summary>
    public async Task<List<string>> GetUserTagsAsync(string userId)
    {
        try
        {
            var websites = await _freeSql.Select<Website>()
                .Where(w => w.UserId == userId && w.Tags != null)
                .ToListAsync(w => w.Tags!);

            var allTags = new HashSet<string>();
            foreach (var tags in websites)
            {
                var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t));
                foreach (var tag in tagArray)
                {
                    allTags.Add(tag);
                }
            }

            return allTags.OrderBy(t => t).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户网站标签失败: {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// 按分类获取网站
    /// </summary>
    public async Task<List<Website>> GetWebsitesByCategoryAsync(string userId, string category)
    {
        try
        {
            return await _freeSql.Select<Website>()
                .Where(w => w.UserId == userId && w.Category == category)
                .Include(w => w.Server)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按分类获取网站失败: {UserId}, {Category}", userId, category);
            return new List<Website>();
        }
    }

    /// <summary>
    /// 按标签获取网站
    /// </summary>
    public async Task<List<Website>> GetWebsitesByTagAsync(string userId, string tag)
    {
        try
        {
            return await _freeSql.Select<Website>()
                .Where(w => w.UserId == userId && w.Tags.Contains(tag))
                .Include(w => w.Server)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按标签获取网站失败: {UserId}, {Tag}", userId, tag);
            return new List<Website>();
        }
    }

    /// <summary>
    /// 更新网站标签
    /// </summary>
    public async Task<bool> UpdateWebsiteTagsAsync(Guid websiteId, string userId, string[] tags)
    {
        try
        {
            var website = await GetWebsiteByIdAsync(websiteId, userId);
            if (website == null)
            {
                return false;
            }

            website.Tags = string.Join(",", tags.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
            return await UpdateWebsiteAsync(website);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站标签失败: {WebsiteId}", websiteId);
            return false;
        }
    }

    /// <summary>
    /// 更新网站分类
    /// </summary>
    public async Task<bool> UpdateWebsiteCategoryAsync(Guid websiteId, string userId, string category)
    {
        try
        {
            var website = await GetWebsiteByIdAsync(websiteId, userId);
            if (website == null)
            {
                return false;
            }

            website.Category = category.Trim();
            return await UpdateWebsiteAsync(website);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站分类失败: {WebsiteId}", websiteId);
            return false;
        }
    }
}
