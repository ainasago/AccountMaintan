namespace WebUI.Services;

/// <summary>
/// CSP验证服务接口
/// </summary>
public interface ICspValidationService
{
    /// <summary>
    /// 验证CSP配置是否有效
    /// </summary>
    /// <param name="cspString">CSP策略字符串</param>
    /// <returns>验证结果</returns>
    CspValidationResult ValidateCsp(string cspString);

    /// <summary>
    /// 获取CSP建议
    /// </summary>
    /// <param name="cspString">CSP策略字符串</param>
    /// <returns>CSP建议</returns>
    List<string> GetCspRecommendations(string cspString);
}

/// <summary>
/// CSP验证结果
/// </summary>
public class CspValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
