using System.ComponentModel.DataAnnotations;

namespace WebUI.Models;

/// <summary>
/// 安全配置选项
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// 内容安全策略配置
    /// </summary>
    public ContentSecurityPolicyOptions ContentSecurityPolicy { get; set; } = new();

    /// <summary>
    /// 开发环境配置
    /// </summary>
    public DevelopmentSecurityOptions Development { get; set; } = new();
}

/// <summary>
/// 内容安全策略配置
/// </summary>
public class ContentSecurityPolicyOptions
{
    /// <summary>
    /// 默认源
    /// </summary>
    public string DefaultSrc { get; set; } = "'self'";

    /// <summary>
    /// 脚本源
    /// </summary>
    public string ScriptSrc { get; set; } = "'self'";

    /// <summary>
    /// 样式源
    /// </summary>
    public string StyleSrc { get; set; } = "'self'";

    /// <summary>
    /// 图片源
    /// </summary>
    public string ImgSrc { get; set; } = "'self'";

    /// <summary>
    /// 字体源
    /// </summary>
    public string FontSrc { get; set; } = "'self'";

    /// <summary>
    /// 连接源
    /// </summary>
    public string ConnectSrc { get; set; } = "'self'";

    /// <summary>
    /// 框架祖先
    /// </summary>
    public string FrameAncestors { get; set; } = "'none'";

    /// <summary>
    /// 基础URI
    /// </summary>
    public string BaseUri { get; set; } = "'self'";

    /// <summary>
    /// 表单动作
    /// </summary>
    public string FormAction { get; set; } = "'self'";

    /// <summary>
    /// 对象源
    /// </summary>
    public string ObjectSrc { get; set; } = "'none'";

    /// <summary>
    /// 媒体源
    /// </summary>
    public string MediaSrc { get; set; } = "'self'";

    /// <summary>
    /// 清单源
    /// </summary>
    public string ManifestSrc { get; set; } = "'self'";

    /// <summary>
    /// Worker源
    /// </summary>
    public string WorkerSrc { get; set; } = "'self'";

    /// <summary>
    /// 子源
    /// </summary>
    public string ChildSrc { get; set; } = "'self'";

    /// <summary>
    /// 框架源
    /// </summary>
    public string FrameSrc { get; set; } = "'none'";

    /// <summary>
    /// 是否升级不安全请求
    /// </summary>
    public bool UpgradeInsecureRequests { get; set; } = false;

    /// <summary>
    /// 生成CSP字符串
    /// </summary>
    /// <returns>CSP策略字符串</returns>
    public string ToCspString()
    {
        var directives = new List<string>();

        if (!string.IsNullOrEmpty(DefaultSrc))
            directives.Add($"default-src {DefaultSrc}");

        if (!string.IsNullOrEmpty(ScriptSrc))
            directives.Add($"script-src {ScriptSrc}");

        if (!string.IsNullOrEmpty(StyleSrc))
            directives.Add($"style-src {StyleSrc}");

        if (!string.IsNullOrEmpty(ImgSrc))
            directives.Add($"img-src {ImgSrc}");

        if (!string.IsNullOrEmpty(FontSrc))
            directives.Add($"font-src {FontSrc}");

        if (!string.IsNullOrEmpty(ConnectSrc))
            directives.Add($"connect-src {ConnectSrc}");

        if (!string.IsNullOrEmpty(FrameAncestors))
            directives.Add($"frame-ancestors {FrameAncestors}");

        if (!string.IsNullOrEmpty(BaseUri))
            directives.Add($"base-uri {BaseUri}");

        if (!string.IsNullOrEmpty(FormAction))
            directives.Add($"form-action {FormAction}");

        if (!string.IsNullOrEmpty(ObjectSrc))
            directives.Add($"object-src {ObjectSrc}");

        if (!string.IsNullOrEmpty(MediaSrc))
            directives.Add($"media-src {MediaSrc}");

        if (!string.IsNullOrEmpty(ManifestSrc))
            directives.Add($"manifest-src {ManifestSrc}");

        if (!string.IsNullOrEmpty(WorkerSrc))
            directives.Add($"worker-src {WorkerSrc}");

        if (!string.IsNullOrEmpty(ChildSrc))
            directives.Add($"child-src {ChildSrc}");

        if (!string.IsNullOrEmpty(FrameSrc))
            directives.Add($"frame-src {FrameSrc}");

        if (UpgradeInsecureRequests)
            directives.Add("upgrade-insecure-requests");

        return string.Join("; ", directives);
    }
}

/// <summary>
/// 开发环境安全配置
/// </summary>
public class DevelopmentSecurityOptions
{
    /// <summary>
    /// 开发环境内容安全策略配置
    /// </summary>
    public ContentSecurityPolicyOptions ContentSecurityPolicy { get; set; } = new();
}
