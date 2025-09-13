using Microsoft.AspNetCore.SignalR;

namespace WebUI.Hubs;

/// <summary>
/// 提醒消息Hub
/// </summary>
public class ReminderHub : Hub
{
    private readonly ILogger<ReminderHub> _logger;

    public ReminderHub(ILogger<ReminderHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 客户端连接
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端 {ConnectionId} 已连接", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开连接
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("客户端 {ConnectionId} 已断开连接", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 加入提醒组
    /// </summary>
    public async Task JoinReminderGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Reminders");
        _logger.LogInformation("客户端 {ConnectionId} 已加入提醒组", Context.ConnectionId);
    }

    /// <summary>
    /// 离开提醒组
    /// </summary>
    public async Task LeaveReminderGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Reminders");
        _logger.LogInformation("客户端 {ConnectionId} 已离开提醒组", Context.ConnectionId);
    }

    /// <summary>
    /// 发送测试提醒
    /// </summary>
    public async Task SendTestReminder(string message)
    {
        _logger.LogInformation("发送测试提醒: {Message}", message);
        
        // 向所有连接的客户端发送提醒
        await Clients.All.SendAsync("ReceiveReminder", message);
    }

    /// <summary>
    /// 发送提醒到指定组
    /// </summary>
    public async Task SendReminderToGroup(string groupName, string message)
    {
        _logger.LogInformation("向组 {GroupName} 发送提醒: {Message}", groupName, message);
        
        await Clients.Group(groupName).SendAsync("ReceiveReminder", message);
    }
}
