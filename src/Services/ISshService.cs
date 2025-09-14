using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// SSH服务接口
/// </summary>
public interface ISshService
{
    /// <summary>
    /// 测试服务器连接
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>连接是否成功</returns>
    Task<bool> TestConnectionAsync(Server server);

    /// <summary>
    /// 执行SSH命令
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="command">要执行的命令</param>
    /// <returns>命令执行结果</returns>
    Task<SshCommandResult> ExecuteCommandAsync(Server server, string command);

    /// <summary>
    /// 获取服务器资源使用情况
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>资源使用情况</returns>
    Task<ServerResourceUsage?> GetResourceUsageAsync(Server server);

    /// <summary>
    /// 重启Supervisor进程
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>操作结果</returns>
    Task<SshCommandResult> RestartSupervisorProcessAsync(Server server, string processName);

    /// <summary>
    /// 获取Supervisor进程状态
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>进程状态</returns>
    Task<string> GetSupervisorProcessStatusAsync(Server server, string processName);

    /// <summary>
    /// 获取所有Supervisor进程状态
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>所有进程状态</returns>
    Task<Dictionary<string, string>> GetAllSupervisorProcessStatusAsync(Server server);

    /// <summary>
    /// 获取网站访问日志
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="logPath">日志文件路径</param>
    /// <param name="lines">获取行数</param>
    /// <returns>日志内容</returns>
    Task<string> GetWebsiteLogAsync(Server server, string logPath, int lines = 100);

    /// <summary>
    /// 检查网站是否可访问
    /// </summary>
    /// <param name="website">网站信息</param>
    /// <returns>是否可访问</returns>
    Task<bool> CheckWebsiteAccessibilityAsync(Website website);
}

/// <summary>
/// SSH命令执行结果
/// </summary>
public class SshCommandResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 输出内容
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 退出代码
    /// </summary>
    public int ExitCode { get; set; }
}

