using WebUI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace WebUI.Services;

/// <summary>
/// 提醒通知服务实现
/// </summary>
public class ReminderNotificationService : IReminderNotificationService
{
    private readonly IHubContext<ReminderHub> _hubContext;
    private readonly ILogger<ReminderNotificationService> _logger;
    private readonly INotificationSettingsService _settingsService;

    public ReminderNotificationService(
        IHubContext<ReminderHub> hubContext,
        ILogger<ReminderNotificationService> logger,
        INotificationSettingsService settingsService)
    {
        _hubContext = hubContext;
        _logger = logger;
        _settingsService = settingsService;
    }

    /// <summary>
    /// 发送提醒通知
    /// </summary>
    public async Task SendReminderNotificationAsync(Guid accountId, string accountName)
    {
        try
        {
            _logger.LogDebug("开始发送账号提醒通知: {AccountName} ({AccountId})", accountName, accountId);

            // 获取通知设置
            var settings = await _settingsService.GetSettingsAsync();
            var tasks = new List<Task>();

            // 根据设置决定发送哪些通知
            if (settings.SignalR.IsEnabled)
            {
                tasks.Add(SendSignalRNotificationAsync(accountId, accountName));
            }

            if (settings.Email.IsEnabled)
            {
                tasks.Add(SendEmailNotificationAsync(accountId, accountName));
            }

            if (settings.Telegram.IsEnabled)
            {
                tasks.Add(SendTelegramNotificationAsync(accountId, accountName));
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            _logger.LogInformation("账号提醒通知发送完成: {AccountName} ({AccountId})", accountName, accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送账号提醒通知失败: {AccountName} ({AccountId})", accountName, accountId);
            throw;
        }
    }

    /// <summary>
    /// 发送SignalR通知
    /// </summary>
    public async Task SendSignalRNotificationAsync(Guid accountId, string accountName)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            var reminder = new
            {
                AccountId = accountId,
                AccountName = accountName,
                Message = $"账号 '{accountName}' 需要访问，请及时登录",
                Timestamp = DateTime.Now,
                Type = "Reminder",
                Title = settings.SignalR.NotificationTitle,
                Icon = settings.SignalR.NotificationIcon
            };

            // 发送到所有连接的客户端
            await _hubContext.Clients.All.SendAsync("ReceiveReminder", reminder);

            _logger.LogInformation("SignalR通知发送成功: {AccountName}", accountName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR通知发送失败: {AccountName}", accountName);
        }
    }

    /// <summary>
    /// 发送邮件通知
    /// </summary>
    public async Task SendEmailNotificationAsync(Guid accountId, string accountName)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (!settings.Email.IsEnabled)
            {
                _logger.LogInformation("邮件通知未启用，跳过邮件通知: {AccountName}", accountName);
                return;
            }

            // 使用模板发送邮件
            var ok = await _settingsService.SendEmailWithTemplateAsync(accountName, accountId.ToString());
            if (ok)
                _logger.LogInformation("邮件通知发送成功: {AccountName}", accountName);
            else
                _logger.LogWarning("邮件通知发送失败: {AccountName}", accountName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件通知发送失败: {AccountName}", accountName);
        }
    }

    /// <summary>
    /// 发送Telegram通知
    /// </summary>
    public async Task SendTelegramNotificationAsync(Guid accountId, string accountName)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (!settings.Telegram.IsEnabled)
            {
                _logger.LogInformation("Telegram通知未启用，跳过Telegram通知: {AccountName}", accountName);
                return;
            }

            // 使用模板发送Telegram消息
            var ok = await _settingsService.SendTelegramWithTemplateAsync(accountName, accountId.ToString());
            if (ok)
                _logger.LogInformation("Telegram通知发送成功: {AccountName}", accountName);
            else
                _logger.LogWarning("Telegram通知发送失败: {AccountName}", accountName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram通知发送失败: {AccountName}", accountName);
        }
    }
}
