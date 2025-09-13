using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemindersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IReminderSchedulerService _schedulerService;
        private readonly IReminderService _reminderService;
        private readonly INotificationSettingsService _settingsService;
        private readonly ILogger<RemindersController> _logger;

        public RemindersController(
            AppDbContext context,
            IReminderSchedulerService schedulerService,
            IReminderService reminderService,
            INotificationSettingsService settingsService,
            ILogger<RemindersController> logger)
        {
            _context = context;
            _schedulerService = schedulerService;
            _reminderService = reminderService;
            _settingsService = settingsService;
            _logger = logger;
        }

        /// <summary>
        /// 获取提醒调度器状态
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            _logger.LogDebug("用户请求获取提醒调度器状态");
            try
            {
                var status = await _schedulerService.GetStatusAsync();
                _logger.LogDebug("成功获取提醒调度器状态");
                return Ok(new { success = true, data = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取提醒调度器状态失败");
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取提醒统计信息
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            _logger.LogDebug("用户请求获取提醒统计信息");
            try
            {
                var totalAccounts = await _context.Fsql.Select<Account>().CountAsync();
                var accountsNeedingReminder = await _reminderService.GetAccountsNeedingReminderAsync();
                var accountsVisitedToday = await _reminderService.GetAccountsVisitedTodayAsync();

                var statistics = new
                {
                    totalAccounts,
                    accountsNeedingReminder = accountsNeedingReminder.Count,
                    accountsVisitedToday
                };

                _logger.LogDebug("统计信息：总账号数 {TotalAccounts}, 需要提醒 {NeedingReminder}, 今日访问 {VisitedToday}", 
                    totalAccounts, accountsNeedingReminder.Count, accountsVisitedToday);

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取提醒统计信息失败");
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取需要提醒的账号列表
        /// </summary>
        [HttpGet("accounts")]
        public async Task<IActionResult> GetReminderAccounts()
        {
            try
            {
                var accounts = await _reminderService.GetAccountsNeedingReminderAsync();
                
                var result = accounts.Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    username = a.Username,
                    daysSinceLastVisit = a.DaysSinceLastVisit,
                    lastVisitDate = a.LastVisited,
                    reminderDays = a.ReminderCycle
                }).ToList();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 手动触发提醒检查
        /// </summary>
        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerReminderCheck()
        {
            try
            {
                await _schedulerService.TriggerReminderCheckAsync();
                return Ok(new { success = true, message = "提醒检查已触发" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 启动提醒调度
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartScheduler()
        {
            try
            {
                await _schedulerService.StartReminderSchedulerAsync();
                return Ok(new { success = true, message = "提醒调度已启动" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 停止提醒调度
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopScheduler()
        {
            try
            {
                await _schedulerService.StopReminderSchedulerAsync();
                return Ok(new { success = true, message = "提醒调度已停止" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取提醒设置
        /// </summary>
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                var reminderSettings = new
                {
                    defaultReminderCycle = settings.Reminder.DefaultReminderCycle,
                    reminderHour = settings.Reminder.ReminderHour,
                    reminderMinute = settings.Reminder.ReminderMinute,
                    checkInterval = settings.Reminder.CheckInterval,
                    enableAutoReminder = settings.Reminder.EnableAutoReminder
                };
                
                return Ok(new { success = true, data = reminderSettings });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 保存提醒设置
        /// </summary>
        [HttpPost("settings")]
        public async Task<IActionResult> SaveSettings([FromBody] ReminderSettingsRequest request)
        {
            try
            {
                
                var settings = await _settingsService.GetSettingsAsync();
                
                // 更新提醒设置
                settings.Reminder.DefaultReminderCycle = request.DefaultReminderCycle;
                settings.Reminder.ReminderHour = request.ReminderHour;
                settings.Reminder.ReminderMinute = request.ReminderMinute;
                
                // 使用请求中的CheckInterval，如果没有则生成默认的
                if (!string.IsNullOrEmpty(request.CheckInterval))
                {
                    settings.Reminder.CheckInterval = request.CheckInterval;
                }
                else
                {
                    // 根据设置生成新的cron表达式
                    var cronExpression = $"0 {request.ReminderMinute} {request.ReminderHour} * * *";
                    settings.Reminder.CheckInterval = cronExpression;
                }
                
                // 保存设置
                var success = await _settingsService.SaveSettingsAsync(settings);
                
                if (success)
                {
                    // 如果调度器正在运行，重新启动以应用新设置
                    try
                    {
                        await _schedulerService.StopReminderSchedulerAsync();
                        await Task.Delay(1000); // 等待1秒
                        await _schedulerService.StartReminderSchedulerAsync();
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不影响设置保存
                        Console.WriteLine($"重新启动调度器失败: {ex.Message}");
                    }
                    
                    return Ok(new { success = true, message = "设置保存成功" });
                }
                else
                {
                    return Ok(new { success = false, message = "保存设置失败" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置账号提醒天数
        /// </summary>
        [HttpPost("accounts/{id}/reminder")]
        public async Task<IActionResult> SetReminderDays(Guid id, [FromBody] SetReminderRequest request)
        {
            try
            {
                var account = await _context.Fsql.Select<Account>().Where(a => a.Id == id).FirstAsync();
                if (account == null)
                {
                    return NotFound(new { success = false, message = "账号不存在" });
                }

                account.ReminderCycle = request.ReminderDays;
                await _context.Fsql.Update<Account>().SetSource(account).ExecuteAffrowsAsync();

                return Ok(new { success = true, message = "提醒设置已更新" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }

    public class SetReminderRequest
    {
        public int ReminderDays { get; set; }
    }

    public class ReminderSettingsRequest
    {
        public int DefaultReminderCycle { get; set; }
        public int ReminderHour { get; set; }
        public int ReminderMinute { get; set; }
        public string CheckInterval { get; set; }
    }
}
