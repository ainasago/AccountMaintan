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
    /// 执行提醒检查（所有用户）
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("开始执行提醒检查作业（所有用户）");

            // 从服务提供者获取服务
            using var scope = _serviceProvider.CreateScope();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

            // 获取所有用户ID（从账号表中获取）
            var allAccounts = await accountService.GetAllAccountsAsync();
            var userIds = allAccounts.Select(a => a.UserId).Distinct().ToList();

            _logger.LogInformation("开始为 {UserCount} 个用户检查提醒", userIds.Count);

            // 为每个用户分别检查提醒
            foreach (var userId in userIds)
            {
                try
                {
                    // 为每个用户创建独立的检查任务
                    BackgroundJob.Enqueue<ReminderCheckJob>(
                        job => job.ExecuteForUserAsync(userId));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "为用户 {UserId} 创建检查任务失败", userId);
                }
            }

            _logger.LogInformation("提醒检查作业执行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提醒检查作业执行失败");
            throw;
        }
    }

    /// <summary>
    /// 为特定用户执行提醒检查
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteForUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("开始为用户 {UserId} 执行提醒检查", userId);

            // 从服务提供者获取服务
            using var scope = _serviceProvider.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IReminderNotificationService>();

            // 检查该用户需要提醒的账号
            var userAccountsNeedingReminder = await reminderService.CheckRemindersAsync(userId);
            
            if (userAccountsNeedingReminder.Any())
            {
                _logger.LogInformation("用户 {UserId} 有 {Count} 个账号需要提醒", userId, userAccountsNeedingReminder.Count);

                // 为每个需要提醒的账号创建通知任务
                foreach (var account in userAccountsNeedingReminder)
                {
                    // 使用 Hangfire 的 BackgroundJob 来异步处理通知
                    BackgroundJob.Enqueue<IReminderNotificationService>(
                        service => service.SendReminderNotificationAsync(account.Id, account.Name, userId));
                }
            }
            else
            {
                _logger.LogDebug("用户 {UserId} 没有需要提醒的账号", userId);
            }

            _logger.LogInformation("用户 {UserId} 的提醒检查执行完成", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为用户 {UserId} 检查提醒失败", userId);
            throw;
        }
    }
}
