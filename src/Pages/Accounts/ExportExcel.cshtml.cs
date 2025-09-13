using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class ExportExcelModel : PageModel
{
    private readonly IAccountService _accountService;

    public ExportExcelModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<IActionResult> OnGet()
    {
        var bytes = await _accountService.ExportAccountsExcelAsync();
        var fileName = $"accounts_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}


