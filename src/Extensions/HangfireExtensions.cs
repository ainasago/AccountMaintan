using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebUI.Models;
using WebUI.Filters;

namespace WebUI.Extensions;

/// <summary>
/// Hangfire 配置扩展
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// 添加 Hangfire 服务
    /// </summary>
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 配置 Hangfire 设置
        var hangfireSettings = new HangfireSettings();
        configuration.GetSection("Hangfire").Bind(hangfireSettings);
        services.Configure<HangfireSettings>(configuration.GetSection("Hangfire"));

        // 添加 Hangfire 服务
        services.AddHangfire(config =>
        {
            // 暂时使用内存存储来避免SQLite配置问题
            config.UseMemoryStorage();
            
            // 配置 Hangfire 选项
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
        });

        // 添加 Hangfire 服务器
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 5;
            options.Queues = new[] { "default", "reminders", "notifications" };
        });

        return services;
    }

    /// <summary>
    /// 配置 Hangfire 中间件
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
    {
        // 获取 Hangfire 设置
        var hangfireSettings = new HangfireSettings();
        configuration.GetSection("Hangfire").Bind(hangfireSettings);

        if (hangfireSettings.EnableDashboard)
        {
            // 配置 Hangfire Dashboard
            app.UseHangfireDashboard(hangfireSettings.DashboardPath, new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter(app.ApplicationServices.GetRequiredService<IOptions<HangfireSettings>>()) },
                DashboardTitle = "账号管理系统 - 任务调度面板"
            });
        }

        return app;
    }
}


