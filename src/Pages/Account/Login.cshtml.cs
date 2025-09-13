using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;
    private readonly IAdminService _adminService;

    public LoginModel(SignInManager<ApplicationUser> signInManager, 
                     UserManager<ApplicationUser> userManager,
                     ILogger<LoginModel> logger,
                     IAdminService adminService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _adminService = adminService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool AllowRegistration { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "密码是必需的")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = "";

        [Display(Name = "记住我")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        // 获取注册设置
        try
        {
            var adminSettings = await _adminService.GetAdminSettingsAsync();
            AllowRegistration = adminSettings.AllowRegistration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取注册设置失败");
            AllowRegistration = false; // 默认不允许注册
        }

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            _logger.LogInformation("尝试登录: {Email}", Input.Email);
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                _logger.LogInformation("找到用户: Id={UserId}, Email={Email}, UserName={UserName}, NormalizedUserName={NormalizedUserName}", 
                    user.Id, user.Email, user.UserName, user.NormalizedUserName);
                
                if (!user.IsEnabled)
                {
                    ModelState.AddModelError(string.Empty, "您的账号已被禁用，请联系管理员。");
                    return Page();
                }
            }
            else
            {
                _logger.LogWarning("未找到邮箱为 {Email} 的用户", Input.Email);
            }

            // 使用用户名进行登录（因为用户名现在就是邮箱）
            var result = await _signInManager.PasswordSignInAsync(user?.UserName ?? Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("用户 {Email} 登录成功。", Input.Email);
                
                // 更新最后登录时间
                if (user != null)
                {
                    user.LastLoginAt = DateTime.Now;
                    await _userManager.UpdateAsync(user);
                }
                
                return LocalRedirect(returnUrl);
            }
            
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("用户 {Email} 账号被锁定。", Input.Email);
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "邮箱或密码错误。");
                return Page();
            }
        }

        return Page();
    }
}
