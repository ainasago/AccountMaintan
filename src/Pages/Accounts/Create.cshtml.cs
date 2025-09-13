using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IAccountService accountService, ILogger<CreateModel> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [BindProperty]
    public WebUI.Models.Account Account { get; set; } = new();

    [BindProperty]
    public List<SecurityQuestion> SecurityQuestions { get; set; } = new();

    public void OnGet()
    {
        // 设置默认值
        Account.ReminderCycle = 30;
        Account.ReminderType = ReminderType.Custom;
        Account.IsActive = true;
        Account.CreatedAt = DateTime.Now;
        
        // 添加一个默认的安全问题
        SecurityQuestions.Add(new SecurityQuestion());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 自定义验证逻辑
        if (string.IsNullOrWhiteSpace(Account.Name))
        {
            ModelState.AddModelError("Account.Name", "账号名称不能为空");
        }

        // 用户名和密码都是可选的，不需要验证
        //if (!ModelState.IsValid)
        //{
        //    return Page();
        //}

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
            if (validSecurityQuestions.Count > 0)
            {
                Account.SecurityQuestions = validSecurityQuestions;
            }
            Account.CreatedAt = DateTime.Now;

            var createdAccount = await _accountService.CreateAccountAsync(Account);
            
            _logger.LogInformation("用户创建了新账号: {AccountName}", createdAccount.Name);
            
            TempData["SuccessMessage"] = $"账号 '{createdAccount.Name}' 创建成功！";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建账号失败");
            ModelState.AddModelError("", "创建账号失败，请重试。");
            return Page();
        }
    }
}
