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
    private readonly IAdminService _adminService;

    public IndexModel(IAccountService accountService, IReminderService reminderService, IAdminService adminService)
    {
        _accountService = accountService;
        _reminderService = reminderService;
        _adminService = adminService;
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
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return;
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            List<WebUI.Models.Account> allAccounts;
            
            if (isAdmin)
            {
                // 管理员可以看到所有账号
                allAccounts = await _accountService.GetAllAccountsAsync();
            }
            else
            {
                // 普通用户只能看到自己的账号
                allAccounts = await _accountService.GetUserAccountsAsync(currentUserId);
            }
            
            // 统计数据
            TotalAccounts = allAccounts.Count;
            ActiveAccounts = allAccounts.Count(a => a.IsActive);
            
            // 获取需要提醒的账号（根据用户权限过滤）
            var accountsNeedingReminder = isAdmin 
                ? await _reminderService.CheckRemindersAsync() 
                : await _reminderService.CheckRemindersAsync(currentUserId);
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
