using Microsoft.AspNetCore.Identity;
using WebUI.Services;

namespace WebUI.Validators;

/// <summary>
/// 自定义密码验证器
/// </summary>
public class CustomPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class
{
    private readonly IPasswordValidatorService _passwordValidatorService;
    private readonly ILogger<CustomPasswordValidator<TUser>> _logger;

    public CustomPasswordValidator(
        IPasswordValidatorService passwordValidatorService,
        ILogger<CustomPasswordValidator<TUser>> logger)
    {
        _passwordValidatorService = passwordValidatorService;
        _logger = logger;
    }

    public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordRequired",
                Description = "密码不能为空"
            });
        }

        try
        {
            // 获取用户名用于密码验证
            var userName = await manager.GetUserNameAsync(user) ?? "";
            
            // 使用自定义密码验证服务
            var validationResult = await _passwordValidatorService.ValidatePasswordAsync(password, userName);
            
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(error => new IdentityError
                {
                    Code = "PasswordValidation",
                    Description = error
                }).ToArray();

                _logger.LogWarning("密码验证失败: 用户={UserName}, 强度={StrengthScore}, 错误={Errors}", 
                    userName, validationResult.StrengthScore, string.Join("; ", validationResult.Errors));

                return IdentityResult.Failed(errors);
            }

            _logger.LogDebug("密码验证通过: 用户={UserName}, 强度={StrengthScore}, 级别={StrengthLevel}", 
                userName, validationResult.StrengthScore, validationResult.StrengthLevel);

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码验证过程中发生错误");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordValidationError",
                Description = "密码验证过程中发生错误，请重试"
            });
        }
    }
}
