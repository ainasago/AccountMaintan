using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class TemplateExcelModel : PageModel
{
    private readonly IAccountService _accountService;

    public TemplateExcelModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<IActionResult> OnGet()
    {
        var bytes = await _accountService.GenerateAccountsExcelTemplateAsync();
        var fileName = "accounts_template.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}


