using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Pages.Accounts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAccountService _accountService;

    public IndexModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public List<WebUI.Models.Account> Accounts { get; set; } = new();
    public AccountSearchModel SearchModel { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;

    public async Task OnGetAsync(string? keyword = null, string? category = null, string? tags = null, int page = 1)
    {
        SearchModel.Keyword = keyword ?? "";
        SearchModel.Category = category ?? "";
        SearchModel.Tags = tags ?? "";
        CurrentPage = Math.Max(1, page);

        try
        {
            // 获取总数
            var allAccounts = await _accountService.SearchAccountsAsync(
                SearchModel.Keyword, 
                SearchModel.Category, 
                SearchModel.Tags);
            
            TotalCount = allAccounts.Count;
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            // 分页获取数据
            var skip = (CurrentPage - 1) * PageSize;
            Accounts = allAccounts.Skip(skip).Take(PageSize).ToList();
            
            // 添加调试信息
            Console.WriteLine($"获取到 {TotalCount} 个账号，当前页显示 {Accounts.Count} 个");
            if (Accounts.Any())
            {
                Console.WriteLine($"第一个账号: {Accounts.First().Name} - {Accounts.First().Username}");
            }
        }
        catch (Exception ex)
        {
            // 记录错误日志
            Console.WriteLine($"获取账号列表失败: {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            Accounts = new List<WebUI.Models.Account>();
        }
    }

    public class AccountSearchModel
    {
        public string Keyword { get; set; } = "";
        public string Category { get; set; } = "";
        public string Tags { get; set; } = "";
    }
}
