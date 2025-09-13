using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace WebUI.Services;

/// <summary>
/// 密码验证服务
/// </summary>
public interface IPasswordValidatorService
{
    /// <summary>
    /// 验证密码强度
    /// </summary>
    Task<PasswordValidationResult> ValidatePasswordAsync(string password, string userName);
    
    /// <summary>
    /// 检查密码是否在常见弱密码列表中
    /// </summary>
    bool IsWeakPassword(string password);
    
    /// <summary>
    /// 计算密码强度分数
    /// </summary>
    int CalculatePasswordStrength(string password);
}

/// <summary>
/// 密码验证结果
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StrengthScore { get; set; }
    public string StrengthLevel { get; set; } = "弱";
}

/// <summary>
/// 密码验证服务实现
/// </summary>
public class PasswordValidatorService : IPasswordValidatorService
{
    private readonly ILogger<PasswordValidatorService> _logger;
    
    // 常见弱密码列表
    private readonly HashSet<string> _weakPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "123456789", "qwerty", "abc123", "password123",
        "admin", "letmein", "welcome", "monkey", "1234567890", "password1",
        "qwerty123", "dragon", "master", "hello", "freedom", "whatever",
        "qazwsx", "trustno1", "jordan23", "harley", "ranger", "jennifer",
        "hunter", "buster", "soccer", "hockey", "killer", "george",
        "sexy", "andrew", "charlie", "superman", "asshole", "fuckyou",
        "dallas", "jessica", "panties", "pepper", "1234", "696969",
        "killer", "george", "sexy", "andrew", "charlie", "superman"
    };

    public PasswordValidatorService(ILogger<PasswordValidatorService> logger)
    {
        _logger = logger;
    }

    public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, string userName)
    {
        var result = new PasswordValidationResult();
        
        if (string.IsNullOrEmpty(password))
        {
            result.Errors.Add("密码不能为空");
            return result;
        }

        // 基本长度检查
        if (password.Length < 12)
        {
            result.Errors.Add("密码长度至少需要12个字符");
        }

        // 检查是否包含用户名
        if (!string.IsNullOrEmpty(userName) && password.Contains(userName, StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("密码不能包含用户名");
        }

        // 检查字符类型
        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            result.Errors.Add("密码必须包含至少一个小写字母");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            result.Errors.Add("密码必须包含至少一个大写字母");
        }

        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            result.Errors.Add("密码必须包含至少一个数字");
        }

        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
        {
            result.Errors.Add("密码必须包含至少一个特殊字符");
        }

        // 检查重复字符
        if (HasRepeatingCharacters(password, 3))
        {
            result.Errors.Add("密码不能包含超过2个连续相同字符");
        }

        // 检查常见模式
        if (HasCommonPatterns(password))
        {
            result.Errors.Add("密码不能包含常见的键盘模式（如qwerty、asdf等）");
        }

        // 检查弱密码
        if (IsWeakPassword(password))
        {
            result.Errors.Add("密码过于简单，请使用更复杂的密码");
        }

        // 计算强度分数
        result.StrengthScore = CalculatePasswordStrength(password);
        result.StrengthLevel = GetStrengthLevel(result.StrengthScore);

        // 如果强度分数低于60，添加警告
        if (result.StrengthScore < 60)
        {
            result.Errors.Add("密码强度不足，建议使用更复杂的密码");
        }

        result.IsValid = result.Errors.Count == 0;
        
        _logger.LogDebug("密码验证结果: 强度={StrengthScore}, 级别={StrengthLevel}, 有效={IsValid}", 
            result.StrengthScore, result.StrengthLevel, result.IsValid);

        return await Task.FromResult(result);
    }

    public bool IsWeakPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return true;

        // 检查是否在弱密码列表中
        if (_weakPasswords.Contains(password))
            return true;

        // 检查是否只包含数字
        if (Regex.IsMatch(password, @"^\d+$"))
            return true;

        // 检查是否只包含字母
        if (Regex.IsMatch(password, @"^[a-zA-Z]+$"))
            return true;

        // 检查是否只包含相同字符
        if (password.Distinct().Count() <= 2)
            return true;

        return false;
    }

    public int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        int score = 0;

        // 长度分数 (最高40分)
        if (password.Length >= 12) score += 20;
        if (password.Length >= 16) score += 10;
        if (password.Length >= 20) score += 10;

        // 字符类型分数 (最高30分)
        if (Regex.IsMatch(password, @"[a-z]")) score += 5;
        if (Regex.IsMatch(password, @"[A-Z]")) score += 5;
        if (Regex.IsMatch(password, @"[0-9]")) score += 5;
        if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score += 10;
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?\\"":{}|<>]")) score += 5;

        // 复杂度分数 (最高20分)
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars >= 8) score += 10;
        if (uniqueChars >= 12) score += 10;

        // 模式检查 (最高10分)
        if (!HasRepeatingCharacters(password, 2)) score += 5;
        if (!HasCommonPatterns(password)) score += 5;

        return Math.Min(score, 100);
    }

    private bool HasRepeatingCharacters(string password, int maxRepeats)
    {
        for (int i = 0; i <= password.Length - maxRepeats; i++)
        {
            var substring = password.Substring(i, maxRepeats);
            if (substring.All(c => c == substring[0]))
                return true;
        }
        return false;
    }

    private bool HasCommonPatterns(string password)
    {
        var commonPatterns = new[]
        {
            "qwerty", "asdf", "zxcv", "1234", "abcd", "qwer",
            "tyui", "uiop", "hjkl", "bnm", "0987", "6543"
        };

        var lowerPassword = password.ToLower();
        return commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }

    private string GetStrengthLevel(int score)
    {
        return score switch
        {
            >= 80 => "强",
            >= 60 => "中等",
            >= 40 => "弱",
            _ => "极弱"
        };
    }
}
