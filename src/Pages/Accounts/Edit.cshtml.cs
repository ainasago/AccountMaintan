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
    private readonly IAdminService _adminService;

    public EditModel(IAccountService accountService, ILogger<EditModel> logger, IAdminService adminService)
    {
        _accountService = accountService;
        _logger = logger;
        _adminService = adminService;
    }

    [BindProperty]
    public WebUI.Models.Account Account { get; set; } = new();

    [BindProperty]
    public List<SecurityQuestion> SecurityQuestions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            WebUI.Models.Account? account;
            
            if (isAdmin)
            {
                // 管理员可以编辑任何账号
                account = await _accountService.GetAccountByIdAsync(id);
            }
            else
            {
                // 普通用户只能编辑自己的账号
                account = await _accountService.GetUserAccountByIdAsync(id, currentUserId);
            }

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

        // 用户名和密码都是可选的，不需要验证
        // 只验证账号名称是否为空
        if (string.IsNullOrWhiteSpace(Account.Name))
        {
            return Page();
        }

        try
        {
            // 设置用户ID
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != null)
            {
                Account.UserId = currentUserId;
            }

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
