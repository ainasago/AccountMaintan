using Quartz;

namespace WebUI.Extensions;

/// <summary>
/// Quartz扩展方法
/// </summary>
public static class QuartzExtensions
{
    /// <summary>
    /// 添加作业和触发器
    /// </summary>
    public static void AddJobAndTrigger<T>(
        this IServiceCollectionQuartzConfigurator quartz,
        Action<QuartzJobOptions>? configure = null) where T : class, IJob
    {
        var options = new QuartzJobOptions();
        configure?.Invoke(options);

        var jobKey = options.JobKey ?? new JobKey(typeof(T).Name);
        var cronExpression = options.CronExpression ?? "0 0 9 * * ?"; // 默认每天上午9点

        quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity(jobKey.Name + "_trigger")
            .WithCronSchedule(cronExpression));
    }
}

/// <summary>
/// Quartz作业选项
/// </summary>
public class QuartzJobOptions
{
    public JobKey? JobKey { get; set; }
    public string? CronExpression { get; set; }
}
