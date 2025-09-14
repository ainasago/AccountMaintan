using FreeSql;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Services;

/// <summary>
/// 服务器管理服务实现
/// </summary>
public class ServerService : IServerService
{
    private readonly IFreeSql _freeSql;
    private readonly ISshService _sshService;
    private readonly ISupervisorService _supervisorService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ServerService> _logger;

    public ServerService(
        IFreeSql freeSql,
        ISshService sshService,
        ISupervisorService supervisorService,
        IEncryptionService encryptionService,
        ILogger<ServerService> logger)
    {
        _freeSql = freeSql;
        _sshService = sshService;
        _supervisorService = supervisorService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<List<Server>> GetServersByUserIdAsync(string userId)
    {
        try
        {
            return await _freeSql.Select<Server>()
                .Where(s => s.UserId == userId)
                .IncludeMany(s => s.Websites)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户服务器列表失败: {UserId}", userId);
            return new List<Server>();
        }
    }

    public async Task<Server?> GetServerByIdAsync(Guid id, string userId)
    {
        try
        {
            return await _freeSql.Select<Server>()
                .Where(s => s.Id == id && s.UserId == userId)
                .IncludeMany(s => s.Websites)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器信息失败: {ServerId}", id);
            return null;
        }
    }

    public async Task<bool> CreateServerAsync(Server server)
    {
        try
        {
            // 加密SSH密码
            if (!string.IsNullOrEmpty(server.SshPassword))
            {
                server.SshPassword = _encryptionService.Encrypt(server.SshPassword);
            }

            var result = await _freeSql.Insert(server).ExecuteAffrowsAsync();
            if (result > 0)
            {
                _logger.LogInformation("服务器创建成功: {ServerName}", server.Name);
                return true;
            }
            else
            {
                _logger.LogWarning("服务器创建失败，影响行数为0: {ServerName}", server.Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建服务器失败: {ServerName}", server.Name);
            return false;
        }
    }

    public async Task<bool> UpdateServerAsync(Server server)
    {
        try
        {
            // 加密SSH密码
            if (!string.IsNullOrEmpty(server.SshPassword))
            {
                server.SshPassword = _encryptionService.Encrypt(server.SshPassword);
            }

            var result = await _freeSql.Update<Server>()
                .SetSource(server)
                .Where(s => s.Id == server.Id && s.UserId == server.UserId)
                .ExecuteAffrowsAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("服务器更新成功: {ServerName}", server.Name);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新服务器失败: {ServerName}", server.Name);
            return false;
        }
    }

    public async Task<bool> DeleteServerAsync(Guid id, string userId)
    {
        try
        {
            // 先删除相关的网站
            await _freeSql.Delete<Website>().Where(w => w.ServerId == id).ExecuteAffrowsAsync();
            
            // 删除服务器
            var result = await _freeSql.Delete<Server>()
                .Where(s => s.Id == id && s.UserId == userId)
                .ExecuteAffrowsAsync();
            
            if (result > 0)
            {
                _logger.LogInformation("服务器删除成功: {ServerId}", id);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除服务器失败: {ServerId}", id);
            return false;
        }
    }

    public async Task<bool> TestServerConnectionAsync(Server server)
    {
        try
        {
            var isConnected = await _sshService.TestConnectionAsync(server);
            
            // 更新连接状态
            server.ConnectionStatus = isConnected ? "Connected" : "Failed";
            server.LastConnectedAt = DateTime.Now;
            
            await _freeSql.Update<Server>()
                .Set(s => s.ConnectionStatus, server.ConnectionStatus)
                .Set(s => s.LastConnectedAt, server.LastConnectedAt)
                .Where(s => s.Id == server.Id)
                .ExecuteAffrowsAsync();
            
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试服务器连接失败: {ServerName}", server.Name);
            return false;
        }
    }

    public async Task<ServerResourceUsage?> GetServerResourceUsageAsync(Server server)
    {
        try
        {
            var resourceUsage = await _sshService.GetResourceUsageAsync(server);
            
            if (resourceUsage != null)
            {
                // 保存资源使用情况到数据库
                await _freeSql.Insert(resourceUsage).ExecuteAffrowsAsync();
            }
            
            return resourceUsage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器资源使用情况失败: {ServerName}", server.Name);
            return null;
        }
    }

    public async Task<SshCommandResult> ExecuteServerCommandAsync(Server server, string command)
    {
        try
        {
            return await _sshService.ExecuteCommandAsync(server, command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行服务器命令失败: {Command}", command);
            return new SshCommandResult
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }

    public async Task<Dictionary<string, SupervisorProcessInfo>> GetServerProcessesAsync(Server server)
    {
        try
        {
            return await _supervisorService.GetAllProcessesAsync(server);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器进程列表失败: {ServerName}", server.Name);
            return new Dictionary<string, SupervisorProcessInfo>();
        }
    }

    public async Task<bool> RestartServerAsync(Server server)
    {
        try
        {
            var command = "sudo reboot";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启服务器失败: {ServerName}", server.Name);
            return false;
        }
    }

    public async Task<string> GetServerLogAsync(Server server, string logType = "syslog", int lines = 100)
    {
        try
        {
            string logPath;
            switch (logType.ToLower())
            {
                case "syslog":
                    logPath = "/var/log/syslog";
                    break;
                case "auth":
                    logPath = "/var/log/auth.log";
                    break;
                case "kern":
                    logPath = "/var/log/kern.log";
                    break;
                case "nginx":
                    logPath = "/var/log/nginx/error.log";
                    break;
                default:
                    logPath = "/var/log/syslog";
                    break;
            }

            return await _sshService.GetWebsiteLogAsync(server, logPath, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器日志失败: {ServerName}", server.Name);
            return $"获取日志失败: {ex.Message}";
        }
    }

    public async Task<Dictionary<Guid, bool>> BatchCheckServerStatusAsync(string userId)
    {
        try
        {
            var servers = await GetServersByUserIdAsync(userId);
            var statusDict = new Dictionary<Guid, bool>();

            foreach (var server in servers)
            {
                var isConnected = await TestServerConnectionAsync(server);
                statusDict[server.Id] = isConnected;
            }

            return statusDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量检查服务器状态失败: {UserId}", userId);
            return new Dictionary<Guid, bool>();
        }
    }

    public async Task<List<ServerResourceUsage>> GetServerResourceHistoryAsync(Guid serverId, string userId, int hours = 24)
    {
        try
        {
            var startTime = DateTime.Now.AddHours(-hours);
            
            return await _freeSql.Select<ServerResourceUsage>()
                .Where(sru => sru.ServerId == serverId && sru.UserId == userId)
                .Where(sru => sru.RecordTime >= startTime)
                .OrderBy(sru => sru.RecordTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器资源历史数据失败: {ServerId}", serverId);
            return new List<ServerResourceUsage>();
        }
    }
}
