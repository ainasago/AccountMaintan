using Quartz;
using WebUI.Data;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 提醒服务接口
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// 检查需要提醒的账号
    /// </summary>
    Task<List<Account>> CheckRemindersAsync();
    Task<List<Account>> CheckRemindersAsync(string? userId = null);
    
    /// <summary>
    /// 发送提醒
    /// </summary>
    Task SendRemindersAsync(List<Account> accounts);
    
    /// <summary>
    /// 获取需要提醒的账号列表
    /// </summary>
    Task<List<Account>> GetAccountsNeedingReminderAsync();
    
    /// <summary>
    /// 获取今日已访问的账号数量
    /// </summary>
    Task<int> GetAccountsVisitedTodayAsync();
}

/// <summary>
/// 提醒服务实现
/// </summary>
public class ReminderService : IReminderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(AppDbContext context, ILogger<ReminderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Account>> CheckRemindersAsync()
    {
        _logger.LogDebug("开始检查需要提醒的账号");
        var now = DateTime.Now;
        var accountsNeedingReminder = new List<Account>();

        try
        {
            var accounts = await _context.Fsql.Select<Account>()
                .Where(a => a.IsActive && a.ReminderCycle > 0)
                .ToListAsync();

            _logger.LogDebug("找到 {Count} 个启用了提醒的账号", accounts.Count);

            foreach (var account in accounts)
            {
                if (ShouldSendReminder(account, now))
                {
                    accountsNeedingReminder.Add(account);
                    _logger.LogDebug("账号 {AccountName} 需要提醒，已超过 {Days} 天未访问", 
                        account.Name, account.ReminderCycle);
                }
            }

            _logger.LogDebug("检查完成，共有 {Count} 个账号需要提醒", accountsNeedingReminder.Count);
            return accountsNeedingReminder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查提醒失败");
            throw;
        }
    }

    public async Task<List<Account>> CheckRemindersAsync(string? userId = null)
    {
        _logger.LogDebug("开始检查需要提醒的账号，用户ID: {UserId}", userId ?? "所有用户");
        var now = DateTime.Now;
        var accountsNeedingReminder = new List<Account>();

        try
        {
            var query = _context.Fsql.Select<Account>()
                .Where(a => a.IsActive && a.ReminderCycle > 0);

            // 如果指定了用户ID，只检查该用户的账号
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            var accounts = await query.ToListAsync();

            _logger.LogDebug("找到 {Count} 个启用了提醒的账号", accounts.Count);

            foreach (var account in accounts)
            {
                if (ShouldSendReminder(account, now))
                {
                    accountsNeedingReminder.Add(account);
                    _logger.LogDebug("账号 {AccountName} 需要提醒，已超过 {Days} 天未访问", 
                        account.Name, account.ReminderCycle);
                }
            }

            _logger.LogDebug("检查完成，共有 {Count} 个账号需要提醒", accountsNeedingReminder.Count);
            return accountsNeedingReminder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查提醒失败");
            throw;
        }
    }

    public async Task SendRemindersAsync(List<Account> accounts)
    {
        foreach (var account in accounts)
        {
            try
            {
                // 记录提醒活动
                var activity = new AccountActivity
                {
                    AccountId = account.Id,
                    ActivityType = ActivityType.Reminder,
                    Description = $"养号提醒：账号 {account.Name} 已超过 {account.ReminderCycle} 天未访问",
                    ActivityTime = DateTime.Now
                };

                await _context.Fsql.Insert(activity).ExecuteAffrowsAsync();

                _logger.LogInformation("已发送养号提醒：{AccountName}", account.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送提醒失败：{AccountName}", account.Name);
            }
        }
    }

    public async Task<List<Account>> GetAccountsNeedingReminderAsync()
    {
        var now = DateTime.Now;
        var accountsNeedingReminder = new List<Account>();

        var accounts = await _context.Fsql.Select<Account>()
            .Where(a => a.IsActive && a.ReminderCycle > 0)
            .ToListAsync();

        foreach (var account in accounts)
        {
            if (ShouldSendReminder(account, now))
            {
                accountsNeedingReminder.Add(account);
            }
        }

        return accountsNeedingReminder;
    }

    public async Task<int> GetAccountsVisitedTodayAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        
        var count = await _context.Fsql.Select<AccountActivity>()
            .Where(a => a.ActivityType == ActivityType.Visit && 
                       a.ActivityTime >= today && 
                       a.ActivityTime < tomorrow)
            .CountAsync();
            
        return (int)count;
    }

    private bool ShouldSendReminder(Account account, DateTime now)
    {
        if (!account.IsActive || account.ReminderCycle <= 0)
            return false;

        if (account.LastVisited == null)
            return true;

        var daysSinceLastVisit = (now - account.LastVisited.Value).TotalDays;
        return daysSinceLastVisit >= account.ReminderCycle;
    }
}

/// <summary>
/// 提醒检查作业
/// </summary>
public class ReminderCheckJob : IJob
{
    private readonly IReminderService _reminderService;
    private readonly ILogger<ReminderCheckJob> _logger;

    public ReminderCheckJob(IReminderService reminderService, ILogger<ReminderCheckJob> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("开始检查养号提醒...");
            
            var accountsNeedingReminder = await _reminderService.CheckRemindersAsync();
            
            if (accountsNeedingReminder.Any())
            {
                await _reminderService.SendRemindersAsync(accountsNeedingReminder);
                _logger.LogInformation("已处理 {Count} 个养号提醒", accountsNeedingReminder.Count);
            }
            else
            {
                _logger.LogInformation("没有需要提醒的账号");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查养号提醒时发生错误");
        }
    }
}
