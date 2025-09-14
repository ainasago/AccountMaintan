using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 服务器管理服务接口
/// </summary>
public interface IServerService
{
    /// <summary>
    /// 获取用户的所有服务器
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>服务器列表</returns>
    Task<List<Server>> GetServersByUserIdAsync(string userId);

    /// <summary>
    /// 根据ID获取服务器
    /// </summary>
    /// <param name="id">服务器ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>服务器信息</returns>
    Task<Server?> GetServerByIdAsync(Guid id, string userId);

    /// <summary>
    /// 创建服务器
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateServerAsync(Server server);

    /// <summary>
    /// 更新服务器
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateServerAsync(Server server);

    /// <summary>
    /// 删除服务器
    /// </summary>
    /// <param name="id">服务器ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteServerAsync(Guid id, string userId);

    /// <summary>
    /// 测试服务器连接
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>连接测试结果</returns>
    Task<bool> TestServerConnectionAsync(Server server);

    /// <summary>
    /// 获取服务器资源使用情况
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>资源使用情况</returns>
    Task<ServerResourceUsage?> GetServerResourceUsageAsync(Server server);

    /// <summary>
    /// 执行服务器命令
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="command">命令</param>
    /// <returns>命令执行结果</returns>
    Task<SshCommandResult> ExecuteServerCommandAsync(Server server, string command);

    /// <summary>
    /// 获取服务器上的所有Supervisor进程
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>进程列表</returns>
    Task<Dictionary<string, SupervisorProcessInfo>> GetServerProcessesAsync(Server server);

    /// <summary>
    /// 重启服务器
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <returns>操作结果</returns>
    Task<bool> RestartServerAsync(Server server);

    /// <summary>
    /// 获取服务器日志
    /// </summary>
    /// <param name="server">服务器信息</param>
    /// <param name="logType">日志类型</param>
    /// <param name="lines">行数</param>
    /// <returns>日志内容</returns>
    Task<string> GetServerLogAsync(Server server, string logType = "syslog", int lines = 100);

    /// <summary>
    /// 批量检查服务器状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>检查结果</returns>
    Task<Dictionary<Guid, bool>> BatchCheckServerStatusAsync(string userId);

    /// <summary>
    /// 获取服务器历史资源使用情况
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="hours">小时数</param>
    /// <returns>历史数据</returns>
    Task<List<ServerResourceUsage>> GetServerResourceHistoryAsync(Guid serverId, string userId, int hours = 24);
}

