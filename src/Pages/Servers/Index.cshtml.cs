using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Servers;

/// <summary>
/// 服务器管理页面
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IServerService _serverService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IServerService serverService, ILogger<IndexModel> logger)
    {
        _serverService = serverService;
        _logger = logger;
    }

    /// <summary>
    /// 服务器列表
    /// </summary>
    public List<Server> Servers { get; set; } = new();

    [BindProperty]
    public Server NewServer { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // 获取用户的服务器列表
            Servers = await _serverService.GetServersByUserIdAsync(userId);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载服务器管理页面失败");
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

            _logger.LogInformation("开始创建服务器，用户ID: {UserId}", userId);
            _logger.LogInformation("服务器信息: Name={Name}, IpAddress={IpAddress}, SshUsername={SshUsername}", 
                NewServer.Name, NewServer.IpAddress, NewServer.SshUsername);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("模型验证失败: {ModelState}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                // 重新加载数据
                Servers = await _serverService.GetServersByUserIdAsync(userId);
                return Page();
            }

            // 设置用户ID
            NewServer.UserId = userId;
            NewServer.Id = Guid.NewGuid();
            NewServer.CreatedAt = DateTime.Now;

            _logger.LogInformation("准备创建服务器，ID: {ServerId}", NewServer.Id);

            // 创建服务器
            var success = await _serverService.CreateServerAsync(NewServer);
            if (success)
            {
                _logger.LogInformation("服务器创建成功: {ServerId}", NewServer.Id);
                TempData["Success"] = "服务器创建成功";
                return RedirectToPage();
            }
            else
            {
                _logger.LogWarning("服务器创建失败: {ServerId}", NewServer.Id);
                TempData["Error"] = "服务器创建失败";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建服务器失败");
            TempData["Error"] = "创建服务器失败，请稍后重试";
        }

        // 重新加载数据
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Servers = await _serverService.GetServersByUserIdAsync(currentUserId);
        return Page();
    }
}
