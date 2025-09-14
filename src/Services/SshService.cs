using Renci.SshNet;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Services;

/// <summary>
/// SSH服务实现
/// </summary>
public class SshService : ISshService
{
    private readonly ILogger<SshService> _logger;
    private readonly IEncryptionService _encryptionService;

    public SshService(ILogger<SshService> logger, IEncryptionService encryptionService)
    {
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<bool> TestConnectionAsync(Server server)
    {
        try
        {
            using var client = CreateSshClient(server);
            await Task.Run(() => client.Connect());
            var isConnected = client.IsConnected;
            client.Disconnect();
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSH连接测试失败: {ServerName}", server.Name);
            return false;
        }
    }

    public async Task<SshCommandResult> ExecuteCommandAsync(Server server, string command)
    {
        try
        {
            using var client = CreateSshClient(server);
            await Task.Run(() => client.Connect());

            if (!client.IsConnected)
            {
                return new SshCommandResult
                {
                    IsSuccess = false,
                    Error = "无法连接到服务器"
                };
            }

            using var cmd = client.CreateCommand(command);
            var result = await Task.Run(() => cmd.Execute());
            
            return new SshCommandResult
            {
                IsSuccess = cmd.ExitStatus == 0,
                Output = result,
                Error = cmd.Error,
                ExitCode = cmd.ExitStatus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSH命令执行失败: {Command}", command);
            return new SshCommandResult
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ServerResourceUsage?> GetResourceUsageAsync(Server server)
    {
        try
        {
            // 获取CPU使用率
            var cpuCommand = "top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | awk -F'%' '{print $1}'";
            var cpuResult = await ExecuteCommandAsync(server, cpuCommand);
            var cpuUsage = double.TryParse(cpuResult.Output.Trim(), out var cpu) ? cpu : 0;

            // 获取内存使用率
            var memoryCommand = "free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'";
            var memoryResult = await ExecuteCommandAsync(server, memoryCommand);
            var memoryUsage = double.TryParse(memoryResult.Output.Trim(), out var mem) ? mem : 0;

            // 获取磁盘使用率
            var diskCommand = "df -h / | awk 'NR==2{print $5}' | sed 's/%//'";
            var diskResult = await ExecuteCommandAsync(server, diskCommand);
            var diskUsage = double.TryParse(diskResult.Output.Trim(), out var disk) ? disk : 0;

            // 获取负载平均值
            var loadCommand = "uptime | awk -F'load average:' '{print $2}' | awk '{print $1}' | sed 's/,//'";
            var loadResult = await ExecuteCommandAsync(server, loadCommand);
            var loadAverage = double.TryParse(loadResult.Output.Trim(), out var load) ? load : 0;

            // 获取运行时间
            var uptimeCommand = "cat /proc/uptime | awk '{print $1}'";
            var uptimeResult = await ExecuteCommandAsync(server, uptimeCommand);
            var uptime = long.TryParse(uptimeResult.Output.Trim().Split('.')[0], out var up) ? up : 0;

            // 获取进程数量
            var processCommand = "ps aux | wc -l";
            var processResult = await ExecuteCommandAsync(server, processCommand);
            var processCount = int.TryParse(processResult.Output.Trim(), out var proc) ? proc : 0;

            return new ServerResourceUsage
            {
                Id = Guid.NewGuid(),
                UserId = server.UserId,
                ServerId = server.Id,
                RecordTime = DateTime.Now,
                CpuUsage = cpuUsage,
                MemoryUsage = memoryUsage,
                DiskUsage = diskUsage,
                LoadAverage = loadAverage,
                Uptime = uptime,
                ProcessCount = processCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器资源使用情况失败: {ServerName}", server.Name);
            return null;
        }
    }

    public async Task<SshCommandResult> RestartSupervisorProcessAsync(Server server, string processName)
    {
        var command = $"sudo supervisorctl restart {processName}";
        return await ExecuteCommandAsync(server, command);
    }

    public async Task<string> GetSupervisorProcessStatusAsync(Server server, string processName)
    {
        var command = $"supervisorctl status {processName}";
        var result = await ExecuteCommandAsync(server, command);
        return result.IsSuccess ? result.Output.Trim() : "Unknown";
    }

    public async Task<Dictionary<string, string>> GetAllSupervisorProcessStatusAsync(Server server)
    {
        var command = "supervisorctl status";
        var result = await ExecuteCommandAsync(server, command);
        
        var statusDict = new Dictionary<string, string>();
        
        if (result.IsSuccess)
        {
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var processName = parts[0];
                    var status = parts[1];
                    statusDict[processName] = status;
                }
            }
        }
        
        return statusDict;
    }

    public async Task<string> GetWebsiteLogAsync(Server server, string logPath, int lines = 100)
    {
        var command = $"tail -n {lines} {logPath}";
        var result = await ExecuteCommandAsync(server, command);
        return result.IsSuccess ? result.Output : result.Error;
    }

    public async Task<bool> CheckWebsiteAccessibilityAsync(Website website)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await httpClient.GetAsync(website.FullUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网站可访问性检查失败: {WebsiteName}", website.Name);
            return false;
        }
    }

    private SshClient CreateSshClient(Server server)
    {
        var client = new SshClient(server.IpAddress, server.SshPort, server.SshUsername, 
            _encryptionService.Decrypt(server.SshPassword ?? ""));
        
        // 设置连接超时
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
        
        return client;
    }
}

