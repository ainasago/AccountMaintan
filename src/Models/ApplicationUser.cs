using Microsoft.AspNetCore.Identity;

namespace WebUI.Models;

/// <summary>
/// 应用程序用户实体
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 是否为管理员
    /// </summary>
    public bool IsAdmin { get; set; } = false;
    
    /// <summary>
    /// 是否为超级管理员（第一个注册的用户）
    /// </summary>
    public bool IsSuperAdmin { get; set; } = false;
}
