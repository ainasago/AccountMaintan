using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly IReminderService _reminderService;

    public IndexModel(IAccountService accountService, IReminderService reminderService)
    {
        _accountService = accountService;
        _reminderService = reminderService;
    }

    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public int AccountsNeedingReminder { get; set; }
    public int OverdueAccounts { get; set; }
    public List<AccountActivity> RecentActivities { get; set; } = new();
    public List<WebUI.Models.Account> AccountsNeedingReminderList { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            // 获取所有账号
            var allAccounts = await _accountService.GetAllAccountsAsync();
            
            // 统计数据
            TotalAccounts = allAccounts.Count;
            ActiveAccounts = allAccounts.Count(a => a.IsActive);
            
            // 获取需要提醒的账号
            var accountsNeedingReminder = await _reminderService.CheckRemindersAsync();
            AccountsNeedingReminder = accountsNeedingReminder.Count;
            AccountsNeedingReminderList = accountsNeedingReminder;
            
            // 计算超期账号数量
            var now = DateTime.Now;
            OverdueAccounts = allAccounts.Count(a => 
                a.IsActive && a.ReminderCycle > 0 && 
                (a.LastVisited == null || a.LastVisited.Value.AddDays(a.ReminderCycle) <= now));

            // 获取最近活动（这里简化处理，实际应该从活动服务获取）
            // RecentActivities = await _activityService.GetRecentActivitiesAsync(10);
        }
        catch (Exception ex)
        {
            // 记录错误日志
            Console.WriteLine($"获取首页数据失败: {ex.Message}");
            
            // 设置默认值
            TotalAccounts = 0;
            ActiveAccounts = 0;
            AccountsNeedingReminder = 0;
            OverdueAccounts = 0;
        }
    }
}
