using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class ImportModel : PageModel
{
    private readonly IAccountService _accountService;

    public ImportModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [BindProperty]
    public IFormFile? File { get; set; }

    public string? ResultMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || File.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "请选择要上传的CSV文件");
            return Page();
        }

        using var stream = File.OpenReadStream();
        (int imported, int skipped) result;
        if (File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            result = await _accountService.ImportAccountsExcelAsync(stream);
        }
        else
        {
            result = await _accountService.ImportAccountsCsvAsync(stream);
        }
        ResultMessage = $"导入完成：成功 {result.imported} 条，跳过 {result.skipped} 条（缺少用户名）。";
        return Page();
    }
}


