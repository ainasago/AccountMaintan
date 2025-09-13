using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class ExportModel : PageModel
{
    private readonly IAccountService _accountService;

    public ExportModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<IActionResult> OnGet()
    {
        var bytes = await _accountService.ExportAccountsCsvAsync();
        var fileName = $"accounts_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}


