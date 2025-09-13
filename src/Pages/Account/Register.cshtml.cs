using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IAdminService _adminService;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IAdminService adminService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _adminService = adminService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public bool AllowRegistration { get; set; }

    public bool IsProcessing { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "显示名称是必需的")]
        [StringLength(100, ErrorMessage = "显示名称不能超过100个字符")]
        [Display(Name = "显示名称")]
        public string DisplayName { get; set; } = "";

        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "密码是必需的")]
        [StringLength(100, ErrorMessage = "密码长度必须在 {2} 到 {1} 个字符之间。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // 检查是否允许注册
        try
        {
            var adminSettings = await _adminService.GetAdminSettingsAsync();
            AllowRegistration = adminSettings.AllowRegistration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取注册设置失败");
            AllowRegistration = false;
        }

        // 如果不允许注册，返回404
        if (!AllowRegistration)
        {
            return NotFound();
        }

        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // 检查是否允许注册
        try
        {
            var adminSettings = await _adminService.GetAdminSettingsAsync();
            AllowRegistration = adminSettings.AllowRegistration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取注册设置失败");
            AllowRegistration = false;
        }

        // 如果不允许注册，返回404
        if (!AllowRegistration)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IsProcessing = true;
            _logger.LogInformation("尝试注册新用户: {Email}", Input.Email);
            
            // 检查用户是否已经存在
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "该邮箱地址已经被注册，请使用其他邮箱或尝试登录。");
                return Page();
            }

            // 检查用户名是否已经存在
            var existingUserByName = await _userManager.FindByNameAsync(Input.Email);
            if (existingUserByName != null)
            {
                ModelState.AddModelError(string.Empty, "该邮箱地址已经被注册，请使用其他邮箱或尝试登录。");
                return Page();
            }
            
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                DisplayName = Input.DisplayName,
                IsEnabled = true,
                CreatedAt = DateTime.Now
            };

            // 检查是否需要管理员审批
            var adminSettings = await _adminService.GetAdminSettingsAsync();
            if (adminSettings.RequireAdminApproval)
            {
                user.IsEnabled = false; // 需要管理员审批
                _logger.LogInformation("用户 {Email} 需要管理员审批", Input.Email);
            }

            var result = await _userManager.CreateAsync(user, Input.Password);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("用户 {Email} 注册成功", Input.Email);
                
                if (adminSettings.RequireAdminApproval)
                {
                    TempData["Message"] = "注册成功！您的账号需要管理员审批后才能使用，请耐心等待。";
                }
                else
                {
                    // 自动登录
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("用户 {Email} 自动登录成功", Input.Email);
                }
                
                return LocalRedirect(returnUrl);
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
    }
}
