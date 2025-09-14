using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// Supervisor服务接口
/// </summary>
public interface ISupervisorService
{
    /// <summary>
    /// 获取所有进程状态
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>进程状态字典</returns>
    Task<Dictionary<string, SupervisorProcessInfo>> GetAllProcessesAsync(Server server);

    /// <summary>
    /// 获取指定进程状态
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>进程信息</returns>
    Task<SupervisorProcessInfo?> GetProcessInfoAsync(Server server, string processName);

    /// <summary>
    /// 启动进程
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>操作结果</returns>
    Task<SupervisorOperationResult> StartProcessAsync(Server server, string processName);

    /// <summary>
    /// 停止进程
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>操作结果</returns>
    Task<SupervisorOperationResult> StopProcessAsync(Server server, string processName);

    /// <summary>
    /// 重启进程
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>操作结果</returns>
    Task<SupervisorOperationResult> RestartProcessAsync(Server server, string processName);

    /// <summary>
    /// 获取进程日志
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <param name="logType">日志类型（stdout/stderr）</param>
    /// <param name="lines">行数</param>
    /// <returns>日志内容</returns>
    Task<string> GetProcessLogAsync(Server server, string processName, string logType = "stdout", int lines = 100);

    /// <summary>
    /// 清除进程日志
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="processName">进程名称</param>
    /// <returns>操作结果</returns>
    Task<SupervisorOperationResult> ClearProcessLogAsync(Server server, string processName);

    /// <summary>
    /// 重新加载配置
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>操作结果</returns>
    Task<SupervisorOperationResult> ReloadConfigAsync(Server server);
}

/// <summary>
/// Supervisor进程信息
/// </summary>
public class SupervisorProcessInfo
{
    /// <summary>
    /// 进程名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 进程ID
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// 运行时间
    /// </summary>
    public string Uptime { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => Status == "RUNNING";

    /// <summary>
    /// 是否已停止
    /// </summary>
    public bool IsStopped => Status == "STOPPED";

    /// <summary>
    /// 是否启动中
    /// </summary>
    public bool IsStarting => Status == "STARTING";

    /// <summary>
    /// 是否已退出
    /// </summary>
    public bool IsExited => Status == "EXITED";

    /// <summary>
    /// 是否异常
    /// </summary>
    public bool IsError => Status == "FATAL" || Status == "BACKOFF";
}

/// <summary>
/// Supervisor操作结果
/// </summary>
public class SupervisorOperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

