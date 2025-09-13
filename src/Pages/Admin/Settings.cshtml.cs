using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Services;

namespace WebUI.Pages.Admin;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IAdminService _adminService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(IAdminService adminService, ILogger<SettingsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public WebUI.Models.AdminSettings Settings { get; set; } = new();
    public int UserCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // 检查是否为管理员
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null || !await _adminService.IsAdminAsync(currentUserId))
            {
                return Forbid();
            }

            Settings = await _adminService.GetAdminSettingsAsync();
            var users = await _adminService.GetAllUsersAsync();
            UserCount = users.Count;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取管理员设置失败");
            return Page();
        }
    }
}
