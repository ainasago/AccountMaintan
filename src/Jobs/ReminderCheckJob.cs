using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using WebUI.Services;

namespace WebUI.Jobs;

/// <summary>
/// 提醒检查作业
/// </summary>
public class ReminderCheckJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderCheckJob> _logger;

    public ReminderCheckJob(
        IServiceProvider serviceProvider,
        ILogger<ReminderCheckJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 执行提醒检查
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("开始执行提醒检查作业");

            // 从服务提供者获取服务
            using var scope = _serviceProvider.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IReminderNotificationService>();

            // 检查需要提醒的账号
            var accountsNeedingReminder = await reminderService.CheckRemindersAsync();
            
            if (accountsNeedingReminder.Any())
            {
                _logger.LogInformation("发现 {Count} 个账号需要提醒", accountsNeedingReminder.Count);

                // 为每个需要提醒的账号创建通知任务
                foreach (var account in accountsNeedingReminder)
                {
                    // 使用 Hangfire 的 BackgroundJob 来异步处理通知
                    BackgroundJob.Enqueue<IReminderNotificationService>(
                        service => service.SendReminderNotificationAsync(account.Id, account.Name));
                }
            }
            else
            {
                _logger.LogInformation("没有账号需要提醒");
            }

            _logger.LogInformation("提醒检查作业执行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提醒检查作业执行失败");
            throw;
        }
    }
}
