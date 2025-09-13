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
    private readonly IAdminService _adminService;

    public IndexModel(IAccountService accountService, IAdminService adminService)
    {
        _accountService = accountService;
        _adminService = adminService;
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
                allAccounts = await _accountService.SearchAccountsAsync(
                    SearchModel.Keyword, 
                    SearchModel.Category, 
                    SearchModel.Tags);
            }
            else
            {
                // 普通用户只能看到自己的账号
                var userAccounts = await _accountService.GetUserAccountsAsync(currentUserId);
                allAccounts = userAccounts.Where(a => 
                    (string.IsNullOrEmpty(SearchModel.Keyword) || 
                     a.Name.Contains(SearchModel.Keyword, StringComparison.OrdinalIgnoreCase) ||
                     (a.Username?.Contains(SearchModel.Keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (a.Notes?.Contains(SearchModel.Keyword, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrEmpty(SearchModel.Category) || 
                     a.Category == SearchModel.Category) &&
                    (string.IsNullOrEmpty(SearchModel.Tags) || 
                     (a.Tags?.Contains(SearchModel.Tags, StringComparison.OrdinalIgnoreCase) ?? false))
                ).ToList();
            }
            
            TotalCount = allAccounts.Count;
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            // 分页获取数据
            var skip = (CurrentPage - 1) * PageSize;
            Accounts = allAccounts.Skip(skip).Take(PageSize).ToList();
            
            // 添加调试信息
            Console.WriteLine($"获取到 {TotalCount} 个账号，当前页显示 {Accounts.Count} 个");
            if (Accounts.Any())
            {
                Console.WriteLine($"第一个账号: {Accounts.First().Name} - {Accounts.First().Username ?? "无用户名"}");
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
