using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebUI.Pages.Settings;

/// <summary>
/// 通知设置页面模型
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // 页面逻辑
    }
}
