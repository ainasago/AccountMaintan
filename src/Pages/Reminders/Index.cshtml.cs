using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Reminders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IReminderService _reminderService;
    private readonly IAccountService _accountService;

    public IndexModel(IReminderService reminderService, IAccountService accountService)
    {
        _reminderService = reminderService;
        _accountService = accountService;
    }

    public List<WebUI.Models.Account> Reminders { get; set; } = new();
    public int OverdueAccounts { get; set; }
    public int DueAccounts { get; set; }
    public int TotalReminders { get; set; }
    public int ProcessedReminders { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // 获取需要提醒的账号
            Reminders = await _reminderService.CheckRemindersAsync();
            
            var now = DateTime.Now;
            
            // 计算统计数据
            OverdueAccounts = Reminders.Count(a => 
                a.LastVisited == null || 
                a.LastVisited.Value.AddDays(a.ReminderCycle) <= now);
                
            DueAccounts = Reminders.Count(a => 
                a.LastVisited.HasValue && 
                a.LastVisited.Value.AddDays(a.ReminderCycle * 0.8) <= now &&
                a.LastVisited.Value.AddDays(a.ReminderCycle) > now);
                
            TotalReminders = Reminders.Count;
            ProcessedReminders = 0; // 这里可以添加已处理提醒的逻辑
        }
        catch (Exception ex)
        {
            // 记录错误日志
            Console.WriteLine($"获取提醒列表失败: {ex.Message}");
            Reminders = new List<WebUI.Models.Account>();
        }
    }
}
