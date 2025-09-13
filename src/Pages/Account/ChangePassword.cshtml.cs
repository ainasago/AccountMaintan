using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebUI.Models;

namespace WebUI.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ChangePasswordModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "当前密码")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        [Display(Name = "邮箱地址")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "密码长度必须在 {2} 到 {1} 个字符之间。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        Input.Email = user.Email ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 自定义验证：如果要修改密码，必须提供当前密码
        if (!string.IsNullOrEmpty(Input.NewPassword) && string.IsNullOrEmpty(Input.CurrentPassword))
        {
            ModelState.AddModelError(string.Empty, "修改密码时必须输入当前密码");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var hasChanges = false;
        var statusMessages = new List<string>();

        // 更新邮箱
        _logger.LogInformation("检查邮箱更新: 当前邮箱={CurrentEmail}, 新邮箱={NewEmail}", user.Email, Input.Email);
        if (user.Email != Input.Email)
        {
            _logger.LogInformation("开始更新邮箱: {CurrentEmail} -> {NewEmail}", user.Email, Input.Email);
            
            // 直接设置邮箱属性
            user.Email = Input.Email;
            user.NormalizedEmail = Input.Email.ToUpperInvariant();
            user.UserName = Input.Email; // 通常用户名也是邮箱
            user.NormalizedUserName = Input.Email.ToUpperInvariant(); // 这是登录时使用的字段
            
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("邮箱更新失败: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"邮箱更新失败: {error.Description}");
                }
                return Page();
            }
            hasChanges = true;
            statusMessages.Add("邮箱已更新");
            _logger.LogInformation("用户 {UserId} 成功修改邮箱为 {Email}", user.Id, Input.Email);
        }
        else
        {
            _logger.LogInformation("邮箱未发生变化，跳过更新");
        }

        // 更新密码（如果提供了新密码）
        if (!string.IsNullOrEmpty(Input.NewPassword))
        {
            if (string.IsNullOrEmpty(Input.CurrentPassword))
            {
                ModelState.AddModelError(string.Empty, "修改密码时必须输入当前密码");
                return Page();
            }

            var passwordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"密码更新失败: {error.Description}");
                }
                return Page();
            }
            hasChanges = true;
            statusMessages.Add("密码已更新");
            _logger.LogInformation("用户 {UserId} 成功修改密码", user.Id);
        }

        if (hasChanges)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = string.Join("，", statusMessages);
        }
        else
        {
            TempData["StatusMessage"] = "没有检测到任何更改";
        }

        return RedirectToPage("/Account/ChangePassword");
    }
}


