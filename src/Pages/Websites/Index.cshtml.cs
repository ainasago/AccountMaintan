using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Websites;

/// <summary>
/// 网站管理页面
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IWebsiteService _websiteService;
    private readonly IServerService _serverService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IWebsiteService websiteService,
        IServerService serverService,
        ILogger<IndexModel> logger)
    {
        _websiteService = websiteService;
        _serverService = serverService;
        _logger = logger;
    }

    /// <summary>
    /// 网站列表
    /// </summary>
    public List<Website> Websites { get; set; } = new();

    /// <summary>
    /// 服务器列表
    /// </summary>
    public List<Server> Servers { get; set; } = new();

    [BindProperty]
    public Website NewWebsite { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // 获取用户的网站列表
            Websites = await _websiteService.GetWebsitesByUserIdAsync(userId);

            // 获取用户的服务器列表
            Servers = await _serverService.GetServersByUserIdAsync(userId);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载网站管理页面失败");
            TempData["Error"] = "加载页面失败，请稍后重试";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                // 重新加载数据
                Websites = await _websiteService.GetWebsitesByUserIdAsync(userId);
                Servers = await _serverService.GetServersByUserIdAsync(userId);
                return Page();
            }

            // 设置用户ID
            NewWebsite.UserId = userId;
            NewWebsite.Id = Guid.NewGuid();
            NewWebsite.CreatedAt = DateTime.Now;

            // 创建网站
            var success = await _websiteService.CreateWebsiteAsync(NewWebsite);
            if (success)
            {
                TempData["Success"] = "网站创建成功";
                return RedirectToPage();
            }
            else
            {
                TempData["Error"] = "网站创建失败";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建网站失败");
            TempData["Error"] = "创建网站失败，请稍后重试";
        }

        // 重新加载数据
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Websites = await _websiteService.GetWebsitesByUserIdAsync(currentUserId);
        Servers = await _serverService.GetServersByUserIdAsync(currentUserId);
        return Page();
    }
}
