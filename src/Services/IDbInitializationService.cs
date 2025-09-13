namespace WebUI.Services;

/// <summary>
/// 数据库初始化服务接口
/// </summary>
public interface IDbInitializationService
{
    /// <summary>
    /// 初始化数据库
    /// </summary>
    Task InitializeAsync();
}
