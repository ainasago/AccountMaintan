using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebUI.Pages.ReminderRecords;

[
    Authorize
]
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // 页面加载时的逻辑
    }
}
