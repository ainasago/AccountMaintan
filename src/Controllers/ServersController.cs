using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// 服务器管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServersController : ControllerBase
{
    private readonly IServerService _serverService;
    private readonly ILogger<ServersController> _logger;

    public ServersController(IServerService serverService, ILogger<ServersController> logger)
    {
        _serverService = serverService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的所有服务器
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Server>>> GetServers()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var servers = await _serverService.GetServersByUserIdAsync(userId);
            return Ok(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器列表失败");
            return StatusCode(500, "获取服务器列表失败");
        }
    }

    /// <summary>
    /// 根据ID获取服务器
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Server>> GetServer(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            return Ok(server);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器信息失败: {ServerId}", id);
            return StatusCode(500, "获取服务器信息失败");
        }
    }

    /// <summary>
    /// 创建服务器
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Server>> CreateServer([FromBody] Server server)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            server.UserId = userId;
            server.Id = Guid.NewGuid();
            server.CreatedAt = DateTime.Now;

            var success = await _serverService.CreateServerAsync(server);
            if (!success)
            {
                return BadRequest("创建服务器失败");
            }

            return CreatedAtAction(nameof(GetServer), new { id = server.Id }, server);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建服务器失败");
            return StatusCode(500, "创建服务器失败");
        }
    }

    /// <summary>
    /// 更新服务器
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServer(Guid id, [FromBody] Server server)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (id != server.Id)
            {
                return BadRequest("ID不匹配");
            }

            server.UserId = userId;
            var success = await _serverService.UpdateServerAsync(server);
            if (!success)
            {
                return BadRequest("更新服务器失败");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新服务器失败: {ServerId}", id);
            return StatusCode(500, "更新服务器失败");
        }
    }

    /// <summary>
    /// 删除服务器
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServer(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _serverService.DeleteServerAsync(id, userId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除服务器失败: {ServerId}", id);
            return StatusCode(500, "删除服务器失败");
        }
    }

    /// <summary>
    /// 测试服务器连接
    /// </summary>
    [HttpPost("{id}/test-connection")]
    public async Task<ActionResult<bool>> TestConnection(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            var isConnected = await _serverService.TestServerConnectionAsync(server);
            return Ok(isConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试服务器连接失败: {ServerId}", id);
            return StatusCode(500, "测试服务器连接失败");
        }
    }

    /// <summary>
    /// 获取服务器资源使用情况
    /// </summary>
    [HttpGet("{id}/resources")]
    public async Task<ActionResult<ServerResourceUsage>> GetServerResources(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            var resources = await _serverService.GetServerResourceUsageAsync(server);
            if (resources == null)
            {
                return NotFound("无法获取服务器资源信息");
            }

            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器资源信息失败: {ServerId}", id);
            return StatusCode(500, "获取服务器资源信息失败");
        }
    }

    /// <summary>
    /// 获取服务器历史资源使用情况
    /// </summary>
    [HttpGet("{id}/resources/history")]
    public async Task<ActionResult<List<ServerResourceUsage>>> GetServerResourceHistory(Guid id, [FromQuery] int hours = 24)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var history = await _serverService.GetServerResourceHistoryAsync(id, userId, hours);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器资源历史数据失败: {ServerId}", id);
            return StatusCode(500, "获取服务器资源历史数据失败");
        }
    }

    /// <summary>
    /// 执行服务器命令
    /// </summary>
    [HttpPost("{id}/execute-command")]
    public async Task<ActionResult<SshCommandResult>> ExecuteCommand(Guid id, [FromBody] ExecuteCommandRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            var result = await _serverService.ExecuteServerCommandAsync(server, request.Command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行服务器命令失败: {ServerId}", id);
            return StatusCode(500, "执行服务器命令失败");
        }
    }

    /// <summary>
    /// 获取服务器进程列表
    /// </summary>
    [HttpGet("{id}/processes")]
    public async Task<ActionResult<Dictionary<string, SupervisorProcessInfo>>> GetServerProcesses(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            var processes = await _serverService.GetServerProcessesAsync(server);
            return Ok(processes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器进程列表失败: {ServerId}", id);
            return StatusCode(500, "获取服务器进程列表失败");
        }
    }

    /// <summary>
    /// 重启服务器
    /// </summary>
    [HttpPost("{id}/restart")]
    public async Task<IActionResult> RestartServer(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            var success = await _serverService.RestartServerAsync(server);
            if (!success)
            {
                return BadRequest("重启服务器失败");
            }

            return Ok(new { message = "服务器重启命令已执行" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启服务器失败: {ServerId}", id);
            return StatusCode(500, "重启服务器失败");
        }
    }

    /// <summary>
    /// 获取服务器日志
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<string>> GetServerLogs(Guid id, [FromQuery] string logType = "syslog", [FromQuery] int lines = 100)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var server = await _serverService.GetServerByIdAsync(id, userId);
            if (server == null)
            {
                return NotFound();
            }

            var logs = await _serverService.GetServerLogAsync(server, logType, lines);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器日志失败: {ServerId}", id);
            return StatusCode(500, "获取服务器日志失败");
        }
    }

    /// <summary>
    /// 批量检查服务器状态
    /// </summary>
    [HttpGet("batch-status")]
    public async Task<ActionResult<Dictionary<Guid, bool>>> BatchCheckStatus()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var statusDict = await _serverService.BatchCheckServerStatusAsync(userId);
            return Ok(statusDict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量检查服务器状态失败");
            return StatusCode(500, "批量检查服务器状态失败");
        }
    }
}

/// <summary>
/// 执行命令请求
/// </summary>
public class ExecuteCommandRequest
{
    /// <summary>
    /// 命令
    /// </summary>
    public string Command { get; set; } = string.Empty;
}


