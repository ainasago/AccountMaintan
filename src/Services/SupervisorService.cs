using WebUI.Models;
using WebUI.Services;

namespace WebUI.Services;

/// <summary>
/// Supervisor服务实现
/// </summary>
public class SupervisorService : ISupervisorService
{
    private readonly ISshService _sshService;
    private readonly ILogger<SupervisorService> _logger;

    public SupervisorService(ISshService sshService, ILogger<SupervisorService> logger)
    {
        _sshService = sshService;
        _logger = logger;
    }

    public async Task<Dictionary<string, SupervisorProcessInfo>> GetAllProcessesAsync(Server server)
    {
        try
        {
            var statusDict = await _sshService.GetAllSupervisorProcessStatusAsync(server);
            var processes = new Dictionary<string, SupervisorProcessInfo>();

            foreach (var kvp in statusDict)
            {
                var processInfo = ParseProcessStatus(kvp.Key, kvp.Value);
                processes[kvp.Key] = processInfo;
            }

            return processes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Supervisor进程列表失败: {ServerName}", server.Name);
            return new Dictionary<string, SupervisorProcessInfo>();
        }
    }

    public async Task<SupervisorProcessInfo?> GetProcessInfoAsync(Server server, string processName)
    {
        try
        {
            var status = await _sshService.GetSupervisorProcessStatusAsync(server, processName);
            return ParseProcessStatus(processName, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Supervisor进程信息失败: {ProcessName}", processName);
            return null;
        }
    }

    public async Task<SupervisorOperationResult> StartProcessAsync(Server server, string processName)
    {
        try
        {
            var command = $"sudo supervisorctl start {processName}";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            
            return new SupervisorOperationResult
            {
                IsSuccess = result.IsSuccess,
                Message = result.IsSuccess ? "进程启动成功" : "进程启动失败",
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动Supervisor进程失败: {ProcessName}", processName);
            return new SupervisorOperationResult
            {
                IsSuccess = false,
                Message = "进程启动失败",
                Error = ex.Message
            };
        }
    }

    public async Task<SupervisorOperationResult> StopProcessAsync(Server server, string processName)
    {
        try
        {
            var command = $"sudo supervisorctl stop {processName}";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            
            return new SupervisorOperationResult
            {
                IsSuccess = result.IsSuccess,
                Message = result.IsSuccess ? "进程停止成功" : "进程停止失败",
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止Supervisor进程失败: {ProcessName}", processName);
            return new SupervisorOperationResult
            {
                IsSuccess = false,
                Message = "进程停止失败",
                Error = ex.Message
            };
        }
    }

    public async Task<SupervisorOperationResult> RestartProcessAsync(Server server, string processName)
    {
        try
        {
            var command = $"sudo supervisorctl restart {processName}";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            
            return new SupervisorOperationResult
            {
                IsSuccess = result.IsSuccess,
                Message = result.IsSuccess ? "进程重启成功" : "进程重启失败",
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启Supervisor进程失败: {ProcessName}", processName);
            return new SupervisorOperationResult
            {
                IsSuccess = false,
                Message = "进程重启失败",
                Error = ex.Message
            };
        }
    }

    public async Task<string> GetProcessLogAsync(Server server, string processName, string logType = "stdout", int lines = 100)
    {
        try
        {
            var command = $"sudo supervisorctl tail {processName} {logType} {lines}";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            return result.IsSuccess ? result.Output : result.Error;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Supervisor进程日志失败: {ProcessName}", processName);
            return $"获取日志失败: {ex.Message}";
        }
    }

    public async Task<SupervisorOperationResult> ClearProcessLogAsync(Server server, string processName)
    {
        try
        {
            var command = $"sudo supervisorctl clear {processName}";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            
            return new SupervisorOperationResult
            {
                IsSuccess = result.IsSuccess,
                Message = result.IsSuccess ? "日志清除成功" : "日志清除失败",
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除Supervisor进程日志失败: {ProcessName}", processName);
            return new SupervisorOperationResult
            {
                IsSuccess = false,
                Message = "日志清除失败",
                Error = ex.Message
            };
        }
    }

    public async Task<SupervisorOperationResult> ReloadConfigAsync(Server server)
    {
        try
        {
            var command = "sudo supervisorctl reread && sudo supervisorctl update";
            var result = await _sshService.ExecuteCommandAsync(server, command);
            
            return new SupervisorOperationResult
            {
                IsSuccess = result.IsSuccess,
                Message = result.IsSuccess ? "配置重新加载成功" : "配置重新加载失败",
                Error = result.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载Supervisor配置失败");
            return new SupervisorOperationResult
            {
                IsSuccess = false,
                Message = "配置重新加载失败",
                Error = ex.Message
            };
        }
    }

    private SupervisorProcessInfo ParseProcessStatus(string processName, string statusLine)
    {
        var parts = statusLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var processInfo = new SupervisorProcessInfo
        {
            Name = processName,
            Status = parts.Length > 0 ? parts[0] : "UNKNOWN",
            Description = statusLine
        };

        // 解析进程ID
        if (parts.Length > 1 && int.TryParse(parts[1], out var pid))
        {
            processInfo.ProcessId = pid;
        }

        // 解析运行时间
        if (parts.Length > 2)
        {
            processInfo.Uptime = parts[2];
        }

        return processInfo;
    }
}

