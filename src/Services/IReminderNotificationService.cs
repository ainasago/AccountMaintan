namespace WebUI.Services;

/// <summary>
/// 提醒通知服务接口
/// </summary>
public interface IReminderNotificationService
{
    /// <summary>
    /// 发送提醒通知
    /// </summary>
    /// <param name="accountId">账号ID</param>
    /// <param name="accountName">账号名称</param>
    /// <param name="userId">用户ID（可选）</param>
    Task SendReminderNotificationAsync(Guid accountId, string accountName, string? userId = null);

    /// <summary>
    /// 发送SignalR通知
    /// </summary>
    /// <param name="accountId">账号ID</param>
    /// <param name="accountName">账号名称</param>
    /// <param name="userId">用户ID（可选）</param>
    Task SendSignalRNotificationAsync(Guid accountId, string accountName, string? userId = null);

    /// <summary>
    /// 发送邮件通知
    /// </summary>
    /// <param name="accountId">账号ID</param>
    /// <param name="accountName">账号名称</param>
    /// <param name="userId">用户ID（可选）</param>
    Task SendEmailNotificationAsync(Guid accountId, string accountName, string? userId = null);

    /// <summary>
    /// 发送Telegram通知
    /// </summary>
    /// <param name="accountId">账号ID</param>
    /// <param name="accountName">账号名称</param>
    /// <param name="userId">用户ID（可选）</param>
    Task SendTelegramNotificationAsync(Guid accountId, string accountName, string? userId = null);
}
