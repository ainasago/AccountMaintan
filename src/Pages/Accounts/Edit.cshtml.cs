using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class EditModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IAccountService accountService, ILogger<EditModel> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [BindProperty]
    public WebUI.Models.Account Account { get; set; } = new();

    [BindProperty]
    public List<SecurityQuestion> SecurityQuestions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            Account = account;
            SecurityQuestions = account.SecurityQuestions?.ToList() ?? new List<SecurityQuestion>();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账号信息失败，ID: {AccountId}", id);
            ModelState.AddModelError("", "获取账号信息失败，请重试。");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 自定义验证逻辑
        if (string.IsNullOrWhiteSpace(Account.Name))
        {
            ModelState.AddModelError("Account.Name", "账号名称不能为空");
        }

        // 清除安全问题相关的验证错误
        foreach (var key in ModelState.Keys.ToList())
        {
            if (key.StartsWith("SecurityQuestions"))
            {
                ModelState.Remove(key);
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // 过滤空的安全问题
            var validSecurityQuestions = SecurityQuestions
                .Where(sq => !string.IsNullOrWhiteSpace(sq.Question) && !string.IsNullOrWhiteSpace(sq.Answer))
                .ToList();

            Account.SecurityQuestions = validSecurityQuestions;

            var result = await _accountService.UpdateAccountAsync(Account);
            if (!result)
            {
                ModelState.AddModelError("", "更新账号失败，账号可能不存在。");
                return Page();
            }

            _logger.LogInformation("用户更新了账号: {AccountName}", Account.Name);
            
            TempData["SuccessMessage"] = $"账号 '{Account.Name}' 更新成功！";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新账号失败，ID: {AccountId}", Account.Id);
            ModelState.AddModelError("", "更新账号失败，请重试。");
            return Page();
        }
    }
}
