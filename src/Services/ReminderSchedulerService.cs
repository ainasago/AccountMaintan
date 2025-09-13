using Hangfire;
using WebUI.Jobs;

namespace WebUI.Services;

/// <summary>
/// 提醒调度服务实现
/// </summary>
public class ReminderSchedulerService : IReminderSchedulerService
{
    private readonly ILogger<ReminderSchedulerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private const string RecurringJobId = "reminder-check-job";
    private const string UserJobIdPrefix = "reminder-check-user-";
    private volatile bool _isRunning;
    private readonly Dictionary<string, bool> _userJobStatus = new();

    public ReminderSchedulerService(
        ILogger<ReminderSchedulerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// 启动提醒调度
    /// </summary>
    public async Task StartReminderSchedulerAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<INotificationSettingsService>();
            var settings = await settingsService.GetSettingsAsync();
            
            if (!settings.Reminder.EnableAutoReminder)
            {
                _logger.LogInformation("自动提醒已禁用，跳过调度启动");
                return;
            }

            // 获取提醒检查间隔
            var checkInterval = settings.Reminder.CheckInterval;
            
            // 如果已经在运行，先停止再重新启动以应用新设置
            if (_isRunning)
            {
                _logger.LogInformation("调度器已在运行，重新启动以应用新设置");
                RecurringJob.RemoveIfExists(RecurringJobId);
                _isRunning = false;
            }

            // 创建/更新定时任务（固定 JobId）
            RecurringJob.AddOrUpdate<WebUI.Jobs.ReminderCheckJob>(
                RecurringJobId,
                job => job.ExecuteAsync(),
                checkInterval);

            _isRunning = true;
            _logger.LogInformation("提醒调度已启动，检查间隔: {Interval}", checkInterval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动提醒调度失败");
            throw;
        }
    }

    /// <summary>
    /// 停止提醒调度
    /// </summary>
    public async Task StopReminderSchedulerAsync()
    {
        try
        {
            RecurringJob.RemoveIfExists(RecurringJobId);
            _isRunning = false;
            _logger.LogInformation("提醒调度已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止提醒调度失败");
            throw;
        }
    }

    /// <summary>
    /// 手动触发提醒检查
    /// </summary>
    public async Task TriggerReminderCheckAsync()
    {
        try
        {
            // 创建一次性任务
            var jobId = BackgroundJob.Enqueue<WebUI.Jobs.ReminderCheckJob>(job => job.ExecuteAsync());
            
            _logger.LogInformation("手动触发提醒检查，任务ID: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动触发提醒检查失败");
            throw;
        }
    }

    /// <summary>
    /// 为特定用户启动提醒调度
    /// </summary>
    public async Task StartUserReminderSchedulerAsync(string userId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<INotificationSettingsService>();
            var settings = await settingsService.GetSettingsAsync();
            
            if (!settings.Reminder.EnableAutoReminder)
            {
                _logger.LogInformation("自动提醒已禁用，跳过用户 {UserId} 的调度启动", userId);
                return;
            }

            var userJobId = $"{UserJobIdPrefix}{userId}";
            var checkInterval = settings.Reminder.CheckInterval;
            
            // 如果已经在运行，先停止再重新启动
            if (_userJobStatus.ContainsKey(userId) && _userJobStatus[userId])
            {
                _logger.LogInformation("用户 {UserId} 的调度器已在运行，重新启动以应用新设置", userId);
                RecurringJob.RemoveIfExists(userJobId);
                _userJobStatus[userId] = false;
            }

            // 创建/更新用户级别的定时任务
            RecurringJob.AddOrUpdate<WebUI.Jobs.ReminderCheckJob>(
                userJobId,
                job => job.ExecuteForUserAsync(userId),
                checkInterval);

            _userJobStatus[userId] = true;
            _logger.LogInformation("用户 {UserId} 的提醒调度已启动，检查间隔: {Interval}", userId, checkInterval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动用户 {UserId} 的提醒调度失败", userId);
            throw;
        }
    }

    /// <summary>
    /// 停止特定用户的提醒调度
    /// </summary>
    public async Task StopUserReminderSchedulerAsync(string userId)
    {
        try
        {
            var userJobId = $"{UserJobIdPrefix}{userId}";
            RecurringJob.RemoveIfExists(userJobId);
            
            if (_userJobStatus.ContainsKey(userId))
            {
                _userJobStatus[userId] = false;
            }
            
            _logger.LogInformation("用户 {UserId} 的提醒调度已停止", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止用户 {UserId} 的提醒调度失败", userId);
            throw;
        }
    }

    /// <summary>
    /// 为特定用户手动触发提醒检查
    /// </summary>
    public async Task TriggerUserReminderCheckAsync(string userId)
    {
        try
        {
            // 创建一次性任务
            var jobId = BackgroundJob.Enqueue<WebUI.Jobs.ReminderCheckJob>(
                job => job.ExecuteForUserAsync(userId));
            
            _logger.LogInformation("手动触发用户 {UserId} 的提醒检查，任务ID: {JobId}", userId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动触发用户 {UserId} 的提醒检查失败", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取调度状态
    /// </summary>
    public async Task<object> GetStatusAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<INotificationSettingsService>();
            var settings = await settingsService.GetSettingsAsync();
            
            // 检查定时任务是否真的在运行
            var isRunning = _isRunning;
            var nextCheck = DateTime.MinValue;
            var intervalDescription = GetIntervalDescription(settings.Reminder.CheckInterval);
            
            // 使用内存状态，并计算下次执行时间
            if (isRunning)
            {
                nextCheck = CalculateNextExecutionTime(settings.Reminder.CheckInterval);
            }
            
            var status = new
            {
                IsRunning = isRunning,
                RecurringJobId = RecurringJobId,
                CheckInterval = settings.Reminder.CheckInterval,
                IntervalDescription = intervalDescription,
                EnableAutoReminder = settings.Reminder.EnableAutoReminder,
                LastCheck = DateTime.Now, // 这里可以添加实际的最后检查时间
                NextCheck = nextCheck,
                PendingJobs = JobStorage.Current.GetMonitoringApi().EnqueuedCount("default"),
                ProcessingJobs = JobStorage.Current.GetMonitoringApi().ProcessingCount()
            };

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取调度状态失败");
            throw;
        }
    }

    /// <summary>
    /// 根据 cron 表达式计算下次执行时间
    /// </summary>
    private DateTime CalculateNextExecutionTime(string cronExpression)
    {
        try
        {
            if (string.IsNullOrEmpty(cronExpression))
                return DateTime.Now.AddHours(1);

            // 简单的 cron 表达式解析（支持基本的格式）
            // 格式：分 时 日 月 周
            var parts = cronExpression.Split(' ');
            if (parts.Length >= 5)
            {
                var minute = parts[0];
                var hour = parts[1];
                var day = parts[2];
                var month = parts[3];
                var weekday = parts[4];

                var now = DateTime.Now;
                var next = now;

                // 处理分钟级别的间隔（如 */15, */30, */45）
                if (minute.StartsWith("*/"))
                {
                    if (int.TryParse(minute.Substring(2), out var minuteInterval))
                    {
                        var currentMinute = now.Minute;
                        var nextMinute = ((currentMinute / minuteInterval) + 1) * minuteInterval;
                        
                        if (nextMinute >= 60)
                        {
                            nextMinute = 0;
                            next = now.AddHours(1);
                        }
                        
                        next = new DateTime(next.Year, next.Month, next.Day, next.Hour, nextMinute, 0);
                        return next;
                    }
                }

                // 处理小时级别的间隔（如 */2, */4, */6）
                if (hour.StartsWith("*/"))
                {
                    if (int.TryParse(hour.Substring(2), out var hourInterval))
                    {
                        var currentHour = now.Hour;
                        var nextHour = ((currentHour / hourInterval) + 1) * hourInterval;
                        
                        if (nextHour >= 24)
                        {
                            nextHour = 0;
                            next = now.AddDays(1);
                        }
                        
                        next = new DateTime(next.Year, next.Month, next.Day, nextHour, 0, 0);
                        return next;
                    }
                }

                // 处理每小时的情况（如 0 * * * *）
                if (hour == "*" && minute != "*")
                {
                    var nextMinute = int.Parse(minute);
                    if (now.Minute >= nextMinute)
                    {
                        next = now.AddHours(1);
                    }
                    next = new DateTime(next.Year, next.Month, next.Day, next.Hour, nextMinute, 0);
                    return next;
                }

                // 处理每天固定时间的情况
                if (day == "*" && month == "*" && weekday == "*")
                {
                    var reminderHour = int.Parse(hour);
                    var reminderMinute = int.Parse(minute);
                    
                    // 如果当前时间已经过了今天的执行时间，计算明天的
                    if (now.Hour > reminderHour || (now.Hour == reminderHour && now.Minute >= reminderMinute))
                    {
                        next = now.AddDays(1);
                    }

                    // 设置执行时间
                    next = new DateTime(next.Year, next.Month, next.Day, reminderHour, reminderMinute, 0);
                    return next;
                }
            }

            // 如果无法解析，返回默认值
            return DateTime.Now.AddHours(1);
        }
        catch
        {
            // 如果解析失败，返回默认值
            return DateTime.Now.AddHours(1);
        }
    }

    /// <summary>
    /// 将 cron 表达式转换为易理解的语言描述
    /// </summary>
    private string GetIntervalDescription(string cronExpression)
    {
        try
        {
            if (string.IsNullOrEmpty(cronExpression))
                return "未设置";

            var parts = cronExpression.Split(' ');
            if (parts.Length >= 5)
            {
                var minute = parts[0];
                var hour = parts[1];
                var day = parts[2];
                var month = parts[3];
                var weekday = parts[4];

                // 处理常见的 cron 表达式
                if (minute == "0" && hour == "9" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每天上午9点";
                }
                else if (minute == "0" && hour == "*/6" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每6小时";
                }
                else if (minute == "0" && hour == "*/12" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每12小时";
                }
                else if (minute == "0" && hour == "*" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每小时";
                }
                else if (minute == "*/30" && hour == "*" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每30分钟";
                }
                else if (minute == "*/15" && hour == "*" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每15分钟";
                }
                else if (minute == "0" && hour == "0" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每天午夜";
                }
                else if (minute == "0" && hour == "12" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每天中午12点";
                }
                else if (minute == "0" && hour == "18" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每天下午6点";
                }
                else if (minute == "0" && hour == "*/2" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每2小时";
                }
                else if (minute == "0" && hour == "*/4" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每4小时";
                }
                else if (minute == "0" && hour == "*/8" && day == "*" && month == "*" && weekday == "*")
                {
                    return "每8小时";
                }
                else
                {
                    // 自定义格式
                    var description = $"每天 {hour}:{minute.PadLeft(2, '0')}";
                    
                    if (day != "*")
                    {
                        description += $"，每月第{day}天";
                    }
                    
                    if (weekday != "*")
                    {
                        var weekNames = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
                        if (int.TryParse(weekday, out var weekIndex) && weekIndex >= 0 && weekIndex < 7)
                        {
                            description += $"，{weekNames[weekIndex]}";
                        }
                    }
                    
                    return description;
                }
            }

            return cronExpression; // 如果无法解析，返回原始表达式
        }
        catch
        {
            return cronExpression; // 如果解析失败，返回原始表达式
        }
    }
}
