using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// 网站管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WebsitesController : ControllerBase
{
    private readonly IWebsiteService _websiteService;
    private readonly ILogger<WebsitesController> _logger;

    public WebsitesController(IWebsiteService websiteService, ILogger<WebsitesController> logger)
    {
        _websiteService = websiteService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的所有网站
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Website>>> GetWebsites()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var websites = await _websiteService.GetWebsitesByUserIdAsync(userId);
            return Ok(websites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站列表失败");
            return StatusCode(500, "获取网站列表失败");
        }
    }

    /// <summary>
    /// 根据ID获取网站
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Website>> GetWebsite(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var website = await _websiteService.GetWebsiteByIdAsync(id, userId);
            if (website == null)
            {
                return NotFound();
            }

            return Ok(website);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站信息失败: {WebsiteId}", id);
            return StatusCode(500, "获取网站信息失败");
        }
    }

    /// <summary>
    /// 创建网站
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Website>> CreateWebsite([FromBody] Website website)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            website.UserId = userId;
            website.Id = Guid.NewGuid();
            website.CreatedAt = DateTime.Now;

            var success = await _websiteService.CreateWebsiteAsync(website);
            if (!success)
            {
                return BadRequest("创建网站失败");
            }

            return CreatedAtAction(nameof(GetWebsite), new { id = website.Id }, website);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建网站失败");
            return StatusCode(500, "创建网站失败");
        }
    }

    /// <summary>
    /// 更新网站
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWebsite(Guid id, [FromBody] Website website)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (id != website.Id)
            {
                return BadRequest("ID不匹配");
            }

            website.UserId = userId;
            var success = await _websiteService.UpdateWebsiteAsync(website);
            if (!success)
            {
                return BadRequest("更新网站失败");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站失败: {WebsiteId}", id);
            return StatusCode(500, "更新网站失败");
        }
    }

    /// <summary>
    /// 删除网站
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebsite(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _websiteService.DeleteWebsiteAsync(id, userId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除网站失败: {WebsiteId}", id);
            return StatusCode(500, "删除网站失败");
        }
    }

    /// <summary>
    /// 检查网站状态
    /// </summary>
    [HttpGet("{id}/status")]
    public async Task<ActionResult<string>> CheckWebsiteStatus(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var website = await _websiteService.GetWebsiteByIdAsync(id, userId);
            if (website == null)
            {
                return NotFound();
            }

            var status = await _websiteService.CheckWebsiteStatusAsync(website);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查网站状态失败: {WebsiteId}", id);
            return StatusCode(500, "检查网站状态失败");
        }
    }

    /// <summary>
    /// 重启网站
    /// </summary>
    [HttpPost("{id}/restart")]
    public async Task<IActionResult> RestartWebsite(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var website = await _websiteService.GetWebsiteByIdAsync(id, userId);
            if (website == null)
            {
                return NotFound();
            }

            var success = await _websiteService.RestartWebsiteAsync(website);
            if (!success)
            {
                return BadRequest("重启网站失败");
            }

            return Ok(new { message = "网站重启成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启网站失败: {WebsiteId}", id);
            return StatusCode(500, "重启网站失败");
        }
    }

    /// <summary>
    /// 获取网站日志
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<string>> GetWebsiteLogs(Guid id, [FromQuery] int lines = 100)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var website = await _websiteService.GetWebsiteByIdAsync(id, userId);
            if (website == null)
            {
                return NotFound();
            }

            var logs = await _websiteService.GetWebsiteLogAsync(website, lines);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站日志失败: {WebsiteId}", id);
            return StatusCode(500, "获取网站日志失败");
        }
    }

    /// <summary>
    /// 批量检查网站状态
    /// </summary>
    [HttpGet("batch-status")]
    public async Task<ActionResult<Dictionary<Guid, string>>> BatchCheckStatus()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var statusDict = await _websiteService.BatchCheckWebsiteStatusAsync(userId);
            return Ok(statusDict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量检查网站状态失败");
            return StatusCode(500, "批量检查网站状态失败");
        }
    }

    /// <summary>
    /// 获取网站账号列表
    /// </summary>
    [HttpGet("{id}/accounts")]
    public async Task<ActionResult<List<WebsiteAccount>>> GetWebsiteAccounts(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var accounts = await _websiteService.GetWebsiteAccountsAsync(id, userId);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站账号列表失败: {WebsiteId}", id);
            return StatusCode(500, "获取网站账号列表失败");
        }
    }

    /// <summary>
    /// 创建网站账号
    /// </summary>
    [HttpPost("{id}/accounts")]
    public async Task<ActionResult<WebsiteAccount>> CreateWebsiteAccount(Guid id, [FromBody] WebsiteAccount account)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            account.WebsiteId = id;
            account.UserId = userId;
            account.Id = Guid.NewGuid();
            account.CreatedAt = DateTime.Now;

            var success = await _websiteService.CreateWebsiteAccountAsync(account);
            if (!success)
            {
                return BadRequest("创建网站账号失败");
            }

            return CreatedAtAction(nameof(GetWebsiteAccounts), new { id }, account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建网站账号失败");
            return StatusCode(500, "创建网站账号失败");
        }
    }

    /// <summary>
    /// 更新网站账号
    /// </summary>
    [HttpPut("{id}/accounts/{accountId}")]
    public async Task<IActionResult> UpdateWebsiteAccount(Guid id, Guid accountId, [FromBody] WebsiteAccount account)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (accountId != account.Id || id != account.WebsiteId)
            {
                return BadRequest("ID不匹配");
            }

            account.UserId = userId;
            var success = await _websiteService.UpdateWebsiteAccountAsync(account);
            if (!success)
            {
                return BadRequest("更新网站账号失败");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站账号失败: {AccountId}", accountId);
            return StatusCode(500, "更新网站账号失败");
        }
    }

    /// <summary>
    /// 删除网站账号
    /// </summary>
    [HttpDelete("{id}/accounts/{accountId}")]
    public async Task<IActionResult> DeleteWebsiteAccount(Guid id, Guid accountId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _websiteService.DeleteWebsiteAccountAsync(accountId, userId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除网站账号失败: {AccountId}", accountId);
            return StatusCode(500, "删除网站账号失败");
        }
    }

    /// <summary>
    /// 获取用户的所有网站分类
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var categories = await _websiteService.GetUserCategoriesAsync(userId);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站分类失败");
            return StatusCode(500, "获取网站分类失败");
        }
    }

    /// <summary>
    /// 获取用户的所有网站标签
    /// </summary>
    [HttpGet("tags")]
    public async Task<ActionResult<List<string>>> GetTags()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var tags = await _websiteService.GetUserTagsAsync(userId);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网站标签失败");
            return StatusCode(500, "获取网站标签失败");
        }
    }

    /// <summary>
    /// 按分类获取网站
    /// </summary>
    [HttpGet("by-category/{category}")]
    public async Task<ActionResult<List<Website>>> GetWebsitesByCategory(string category)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var websites = await _websiteService.GetWebsitesByCategoryAsync(userId, category);
            return Ok(websites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按分类获取网站失败: {Category}", category);
            return StatusCode(500, "按分类获取网站失败");
        }
    }

    /// <summary>
    /// 按标签获取网站
    /// </summary>
    [HttpGet("by-tag/{tag}")]
    public async Task<ActionResult<List<Website>>> GetWebsitesByTag(string tag)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var websites = await _websiteService.GetWebsitesByTagAsync(userId, tag);
            return Ok(websites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按标签获取网站失败: {Tag}", tag);
            return StatusCode(500, "按标签获取网站失败");
        }
    }

    /// <summary>
    /// 更新网站标签
    /// </summary>
    [HttpPut("{id}/tags")]
    public async Task<IActionResult> UpdateWebsiteTags(Guid id, [FromBody] string[] tags)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _websiteService.UpdateWebsiteTagsAsync(id, userId, tags);
            if (!success)
            {
                return BadRequest("更新网站标签失败");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站标签失败: {WebsiteId}", id);
            return StatusCode(500, "更新网站标签失败");
        }
    }

    /// <summary>
    /// 更新网站分类
    /// </summary>
    [HttpPut("{id}/category")]
    public async Task<IActionResult> UpdateWebsiteCategory(Guid id, [FromBody] string category)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _websiteService.UpdateWebsiteCategoryAsync(id, userId, category);
            if (!success)
            {
                return BadRequest("更新网站分类失败");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新网站分类失败: {WebsiteId}", id);
            return StatusCode(500, "更新网站分类失败");
        }
    }
}


