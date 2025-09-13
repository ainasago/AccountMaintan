using Hangfire;

namespace WebUI.Services;

/// <summary>
/// 提醒调度服务接口
/// </summary>
public interface IReminderSchedulerService
{
    /// <summary>
    /// 启动提醒调度
    /// </summary>
    Task StartReminderSchedulerAsync();

    /// <summary>
    /// 停止提醒调度
    /// </summary>
    Task StopReminderSchedulerAsync();

    /// <summary>
    /// 手动触发提醒检查
    /// </summary>
    Task TriggerReminderCheckAsync();

    /// <summary>
    /// 获取调度状态
    /// </summary>
    Task<object> GetStatusAsync();
}
