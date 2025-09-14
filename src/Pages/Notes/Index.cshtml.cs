using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebUI.Pages.Notes;

/// <summary>
/// 笔记管理页面
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // 页面初始化
    }
}
