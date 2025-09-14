using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 网站管理服务接口
/// </summary>
public interface IWebsiteService
{
    /// <summary>
    /// 获取用户的所有网站
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>网站列表</returns>
    Task<List<Website>> GetWebsitesByUserIdAsync(string userId);

    /// <summary>
    /// 根据ID获取网站
    /// </summary>
    /// <param name="id">网站ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>网站信息</returns>
    Task<Website?> GetWebsiteByIdAsync(Guid id, string userId);

    /// <summary>
    /// 创建网站
    /// </summary>
    /// <param name="website">网站信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateWebsiteAsync(Website website);

    /// <summary>
    /// 更新网站
    /// </summary>
    /// <param name="website">网站信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateWebsiteAsync(Website website);

    /// <summary>
    /// 删除网站
    /// </summary>
    /// <param name="id">网站ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteWebsiteAsync(Guid id, string userId);

    /// <summary>
    /// 检查网站状态
    /// </summary>
    /// <param name="website">网站信息</param>
    /// <returns>网站状态</returns>
    Task<string> CheckWebsiteStatusAsync(Website website);

    /// <summary>
    /// 重启网站
    /// </summary>
    /// <param name="website">网站信息</param>
    /// <returns>操作结果</returns>
    Task<bool> RestartWebsiteAsync(Website website);

    /// <summary>
    /// 获取网站访问日志
    /// </summary>
    /// <param name="website">网站信息</param>
    /// <param name="lines">行数</param>
    /// <returns>日志内容</returns>
    Task<string> GetWebsiteLogAsync(Website website, int lines = 100);

    /// <summary>
    /// 记录网站访问
    /// </summary>
    /// <param name="websiteId">网站ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="accessType">访问类型</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="notes">备注</param>
    /// <returns>记录结果</returns>
    Task<bool> LogWebsiteAccessAsync(Guid websiteId, string userId, string accessType, 
        string? ipAddress = null, string? userAgent = null, string? notes = null);

    /// <summary>
    /// 获取网站账号列表
    /// </summary>
    /// <param name="websiteId">网站ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>账号列表</returns>
    Task<List<WebsiteAccount>> GetWebsiteAccountsAsync(Guid websiteId, string userId);

    /// <summary>
    /// 创建网站账号
    /// </summary>
    /// <param name="account">账号信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateWebsiteAccountAsync(WebsiteAccount account);

    /// <summary>
    /// 更新网站账号
    /// </summary>
    /// <param name="account">账号信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateWebsiteAccountAsync(WebsiteAccount account);

    /// <summary>
    /// 删除网站账号
    /// </summary>
    /// <param name="id">账号ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteWebsiteAccountAsync(Guid id, string userId);

    /// <summary>
    /// 批量检查网站状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>检查结果</returns>
    Task<Dictionary<Guid, string>> BatchCheckWebsiteStatusAsync(string userId);

    /// <summary>
    /// 获取用户的所有网站分类
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>分类列表</returns>
    Task<List<string>> GetUserCategoriesAsync(string userId);

    /// <summary>
    /// 获取用户的所有网站标签
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>标签列表</returns>
    Task<List<string>> GetUserTagsAsync(string userId);

    /// <summary>
    /// 按分类获取网站
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="category">分类名称</param>
    /// <returns>网站列表</returns>
    Task<List<Website>> GetWebsitesByCategoryAsync(string userId, string category);

    /// <summary>
    /// 按标签获取网站
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tag">标签名称</param>
    /// <returns>网站列表</returns>
    Task<List<Website>> GetWebsitesByTagAsync(string userId, string tag);

    /// <summary>
    /// 更新网站标签
    /// </summary>
    /// <param name="websiteId">网站ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="tags">标签数组</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateWebsiteTagsAsync(Guid websiteId, string userId, string[] tags);

    /// <summary>
    /// 更新网站分类
    /// </summary>
    /// <param name="websiteId">网站ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="category">分类名称</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateWebsiteCategoryAsync(Guid websiteId, string userId, string category);
}

