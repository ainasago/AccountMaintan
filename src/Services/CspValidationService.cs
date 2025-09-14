using System.Text.RegularExpressions;

namespace WebUI.Services;

/// <summary>
/// CSP验证服务实现
/// </summary>
public class CspValidationService : ICspValidationService
{
    private readonly ILogger<CspValidationService> _logger;

    public CspValidationService(ILogger<CspValidationService> logger)
    {
        _logger = logger;
    }

    public CspValidationResult ValidateCsp(string cspString)
    {
        var result = new CspValidationResult();

        if (string.IsNullOrEmpty(cspString))
        {
            result.Errors.Add("CSP策略不能为空");
            return result;
        }

        // 验证CSP语法
        ValidateCspSyntax(cspString, result);

        // 检查安全风险
        CheckSecurityRisks(cspString, result);

        // 提供建议
        result.Recommendations.AddRange(GetCspRecommendations(cspString));

        result.IsValid = result.Errors.Count == 0;

        return result;
    }

    public List<string> GetCspRecommendations(string cspString)
    {
        var recommendations = new List<string>();

        // 检查是否使用了unsafe-inline
        if (cspString.Contains("'unsafe-inline'"))
        {
            recommendations.Add("考虑移除 'unsafe-inline'，使用nonce或hash来允许特定的内联脚本和样式");
        }

        // 检查是否使用了unsafe-eval
        if (cspString.Contains("'unsafe-eval'"))
        {
            recommendations.Add("考虑移除 'unsafe-eval'，避免使用eval()和Function()构造函数");
        }

        // 检查是否缺少重要的指令
        if (!cspString.Contains("frame-ancestors"))
        {
            recommendations.Add("添加 frame-ancestors 指令以防止点击劫持攻击");
        }

        if (!cspString.Contains("base-uri"))
        {
            recommendations.Add("添加 base-uri 指令以限制base标签的使用");
        }

        if (!cspString.Contains("object-src"))
        {
            recommendations.Add("添加 object-src 'none' 指令以禁用插件");
        }

        // 检查是否使用了通配符
        if (cspString.Contains("*"))
        {
            recommendations.Add("避免使用通配符 '*'，使用具体的域名");
        }

        // 检查是否缺少upgrade-insecure-requests
        if (!cspString.Contains("upgrade-insecure-requests"))
        {
            recommendations.Add("考虑添加 upgrade-insecure-requests 指令以自动升级HTTP到HTTPS");
        }

        return recommendations;
    }

    private void ValidateCspSyntax(string cspString, CspValidationResult result)
    {
        // 基本语法检查
        var directives = cspString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var directive in directives)
        {
            var trimmed = directive.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                result.Errors.Add($"指令格式错误: {trimmed}");
                continue;
            }

            var directiveName = parts[0];
            var sources = parts.Skip(1).ToArray();

            // 验证指令名称
            if (!IsValidDirectiveName(directiveName))
            {
                result.Errors.Add($"未知的指令名称: {directiveName}");
            }

            // 验证源值
            foreach (var source in sources)
            {
                if (!IsValidSourceValue(source))
                {
                    result.Warnings.Add($"可能无效的源值: {source}");
                }
            }
        }
    }

    private void CheckSecurityRisks(string cspString, CspValidationResult result)
    {
        // 检查高风险配置
        if (cspString.Contains("'unsafe-inline'") && cspString.Contains("script-src"))
        {
            result.Warnings.Add("script-src 包含 'unsafe-inline' 可能增加XSS风险");
        }

        if (cspString.Contains("'unsafe-eval'"))
        {
            result.Warnings.Add("使用 'unsafe-eval' 可能增加代码注入风险");
        }

        if (cspString.Contains("data:") && cspString.Contains("script-src"))
        {
            result.Warnings.Add("script-src 包含 'data:' 可能增加XSS风险");
        }

        if (cspString.Contains("*") && cspString.Contains("script-src"))
        {
            result.Warnings.Add("script-src 包含通配符 '*' 可能增加安全风险");
        }

        // 检查缺少的重要指令
        if (!cspString.Contains("frame-ancestors"))
        {
            result.Warnings.Add("缺少 frame-ancestors 指令，可能容易受到点击劫持攻击");
        }

        if (!cspString.Contains("object-src"))
        {
            result.Warnings.Add("缺少 object-src 指令，建议设置为 'none'");
        }
    }

    private bool IsValidDirectiveName(string directiveName)
    {
        var validDirectives = new[]
        {
            "default-src", "script-src", "style-src", "img-src", "font-src",
            "connect-src", "media-src", "object-src", "child-src", "frame-src",
            "worker-src", "manifest-src", "base-uri", "form-action",
            "frame-ancestors", "upgrade-insecure-requests", "block-all-mixed-content"
        };

        return validDirectives.Contains(directiveName);
    }

    private bool IsValidSourceValue(string source)
    {
        // 检查关键字
        var keywords = new[] { "'self'", "'unsafe-inline'", "'unsafe-eval'", "'none'", "'strict-dynamic'" };
        if (keywords.Contains(source))
            return true;

        // 检查协议
        var protocols = new[] { "data:", "blob:", "https:", "http:", "wss:", "ws:" };
        if (protocols.Any(p => source.StartsWith(p)))
            return true;

        // 检查通配符
        if (source == "*")
            return true;

        // 检查域名（简单验证）
        var domainPattern = @"^https?://[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}(/.*)?$";
        if (Regex.IsMatch(source, domainPattern))
            return true;

        // 检查IP地址
        var ipPattern = @"^https?://\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?(/.*)?$";
        if (Regex.IsMatch(source, ipPattern))
            return true;

        return false;
    }
}
