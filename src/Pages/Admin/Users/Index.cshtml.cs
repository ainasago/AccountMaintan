using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Services;

namespace WebUI.Pages.Admin.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAdminService _adminService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IAdminService adminService, ILogger<IndexModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public List<WebUI.Models.ApplicationUser> Users { get; set; } = new();

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

            Users = await _adminService.GetAllUsersAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return Page();
        }
    }
}
