using WebUI.Data;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 账号服务接口
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// 获取所有账号
    /// </summary>
    Task<List<Account>> GetAllAccountsAsync();
    
    /// <summary>
    /// 获取指定用户的所有账号
    /// </summary>
    Task<List<Account>> GetUserAccountsAsync(string userId);
    
    /// <summary>
    /// 根据ID获取账号
    /// </summary>
    Task<Account?> GetAccountByIdAsync(Guid id);
    
    /// <summary>
    /// 根据ID和用户ID获取账号
    /// </summary>
    Task<Account?> GetUserAccountByIdAsync(Guid id, string userId);
    
    /// <summary>
    /// 创建账号
    /// </summary>
    Task<Account> CreateAccountAsync(Account account);
    
    /// <summary>
    /// 更新账号
    /// </summary>
    Task<bool> UpdateAccountAsync(Account account);
    
    /// <summary>
    /// 删除账号
    /// </summary>
    Task<bool> DeleteAccountAsync(Guid id);
    
    /// <summary>
    /// 搜索账号
    /// </summary>
    Task<List<Account>> SearchAccountsAsync(string keyword, string? category = null, string? tags = null);
    
    /// <summary>
    /// 获取需要提醒的账号
    /// </summary>
    Task<List<Account>> GetAccountsNeedingReminderAsync();
    
    /// <summary>
    /// 记录账号访问
    /// </summary>
    Task RecordAccountVisitAsync(Guid accountId, string? ipAddress = null, string? userAgent = null);
    
    /// <summary>
    /// 获取账号活动历史
    /// </summary>
    Task<List<AccountActivity>> GetAccountActivitiesAsync(Guid accountId);

    /// <summary>
    /// 导出所有账号为CSV
    /// </summary>
    Task<byte[]> ExportAccountsCsvAsync();

    /// <summary>
    /// 从CSV导入账号数据
    /// </summary>
    Task<(int imported, int skipped)> ImportAccountsCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出为Excel
    /// </summary>
    Task<byte[]> ExportAccountsExcelAsync();

    /// <summary>
    /// 从Excel导入
    /// </summary>
    Task<(int imported, int skipped)> ImportAccountsExcelAsync(Stream excelStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成Excel模板
    /// </summary>
    Task<byte[]> GenerateAccountsExcelTemplateAsync();
}

/// <summary>
/// 账号服务实现
/// </summary>
public class AccountService : IAccountService
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AppDbContext context, IEncryptionService encryptionService, ILogger<AccountService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<List<Account>> GetAllAccountsAsync()
    {
        _logger.LogDebug("开始获取所有账号");
        try
        {
            var accounts = await _context.Fsql.Select<Account>()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            _logger.LogDebug("成功获取 {Count} 个账号", accounts.Count);
            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有账号失败");
            throw;
        }
    }

    public async Task<List<Account>> GetUserAccountsAsync(string userId)
    {
        _logger.LogDebug("开始获取用户 {UserId} 的账号", userId);
        try
        {
            var accounts = await _context.Fsql.Select<Account>()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            _logger.LogDebug("成功获取用户 {UserId} 的 {Count} 个账号", userId, accounts.Count);
            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 的账号失败", userId);
            throw;
        }
    }

    public async Task<Account?> GetAccountByIdAsync(Guid id)
    {
        _logger.LogDebug("开始获取账号: {AccountId}", id);
        try
        {
            var account = await _context.Fsql.Select<Account>()
                .Where(a => a.Id == id)
                .FirstAsync();
            if (account != null)
            {
                _logger.LogDebug("成功获取账号: {AccountName} ({AccountId})", account.Name, id);
            }
            else
            {
                _logger.LogDebug("账号不存在: {AccountId}", id);
            }
            return account;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账号失败: {AccountId}", id);
            throw;
        }
    }

    public async Task<Account?> GetUserAccountByIdAsync(Guid id, string userId)
    {
        _logger.LogDebug("开始获取用户 {UserId} 的账号: {AccountId}", userId, id);
        try
        {
            var account = await _context.Fsql.Select<Account>()
                .Where(a => a.Id == id && a.UserId == userId)
                .FirstAsync();
            if (account != null)
            {
                _logger.LogDebug("成功获取用户 {UserId} 的账号: {AccountName} ({AccountId})", userId, account.Name, id);
            }
            else
            {
                _logger.LogDebug("用户 {UserId} 的账号不存在: {AccountId}", userId, id);
            }
            return account;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 的账号失败: {AccountId}", userId, id);
            throw;
        }
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        _logger.LogDebug("开始创建账号: {AccountName}", account.Name);
        try
        {
            // 加密敏感信息
            if (!string.IsNullOrEmpty(account.Password))
            {
                account.Password = _encryptionService.Encrypt(account.Password);
                _logger.LogDebug("已加密账号密码");
            }
            else
            {
                // 如果密码为空，设置一个默认的空密码（加密后的空字符串）
                account.Password = _encryptionService.Encrypt(string.Empty);
                _logger.LogDebug("账号密码为空，已设置默认加密密码");
            }
            
            if (!string.IsNullOrEmpty(account.AuthenticatorKey))
            {
                account.AuthenticatorKey = _encryptionService.Encrypt(account.AuthenticatorKey);
                _logger.LogDebug("已加密认证器密钥");
            }

            // 加密安全问题答案（只处理有效的问题）
            if (account.SecurityQuestions != null)
            {
                var validQuestions = account.SecurityQuestions.Where(sq => !string.IsNullOrWhiteSpace(sq.Question) && !string.IsNullOrWhiteSpace(sq.Answer));
                foreach (var question in validQuestions)
                {
                    question.Answer = _encryptionService.Encrypt(question.Answer);
                }
                _logger.LogDebug("已加密 {Count} 个安全问题答案", validQuestions.Count());
            }

            account.CreatedAt = DateTime.Now;
            account.Id = Guid.NewGuid();

            await _context.Fsql.Insert(account).ExecuteAffrowsAsync();
            _logger.LogDebug("成功创建账号: {AccountName} ({AccountId})", account.Name, account.Id);
            return account;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建账号失败: {AccountName}", account.Name);
            throw;
        }
    }

    public async Task<bool> UpdateAccountAsync(Account account)
    {
        var existingAccount = await GetAccountByIdAsync(account.Id);
        if (existingAccount == null) return false;

        // 如果密码没有变化，保持原密码
        if (string.IsNullOrEmpty(account.Password))
        {
            account.Password = existingAccount!.Password;
        }
        else
        {
            account.Password = _encryptionService.Encrypt(account.Password);
        }

        // 如果TOTP密钥没有变化，保持原密钥
        if (string.IsNullOrEmpty(account.AuthenticatorKey))
        {
            account.AuthenticatorKey = existingAccount!.AuthenticatorKey;
        }
        else
        {
            account.AuthenticatorKey = _encryptionService.Encrypt(account.AuthenticatorKey);
        }

        // 处理安全问题（只处理有效的问题）
        if (account.SecurityQuestions != null)
        {
            foreach (var question in account.SecurityQuestions.Where(sq => !string.IsNullOrWhiteSpace(sq.Question) && !string.IsNullOrWhiteSpace(sq.Answer)))
            {
                if (question.Id == Guid.Empty)
                {
                    // 新问题，需要加密答案
                    question.Answer = _encryptionService.Encrypt(question.Answer);
                }
                else
                {
                    // 现有问题，检查答案是否变化
                    var existingQuestion = existingAccount.SecurityQuestions?.FirstOrDefault(sq => sq.Id == question.Id);
                    if (existingQuestion != null && question.Answer != existingQuestion.Answer)
                    {
                        question.Answer = _encryptionService.Encrypt(question.Answer);
                    }
                    else
                    {
                        question.Answer = existingQuestion?.Answer ?? string.Empty;
                    }
                }
            }
        }

        var result = await _context.Fsql.Update<Account>()
            .SetSource(account)
            .ExecuteAffrowsAsync();

        return result > 0;
    }

    public async Task<bool> DeleteAccountAsync(Guid id)
    {
        var result = await _context.Fsql.Delete<Account>()
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync();

        return result > 0;
    }

    public async Task<List<Account>> SearchAccountsAsync(string keyword, string? category = null, string? tags = null)
    {
        try
        {
            var query = _context.Fsql.Select<Account>();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(a => a.Name.Contains(keyword) || 
                                       (a.Username != null && a.Username.Contains(keyword)) || 
                                       (a.Notes != null && a.Notes.Contains(keyword)));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(tags))
            {
                query = query.Where(a => a.Tags != null && a.Tags.Contains(tags));
            }

            var result = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            Console.WriteLine($"SearchAccountsAsync 查询结果: {result.Count} 个账号");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SearchAccountsAsync 查询失败: {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            throw;
        }
    }

    public async Task<List<Account>> GetAccountsNeedingReminderAsync()
    {
        var now = DateTime.Now;
        return await _context.Fsql.Select<Account>()
            .Where(a => a.IsActive && a.ReminderCycle > 0)
            .Where(a => a.LastVisited == null || 
                       a.LastVisited.Value.AddDays(a.ReminderCycle) <= now)
            .ToListAsync();
    }

    public async Task RecordAccountVisitAsync(Guid accountId, string? ipAddress = null, string? userAgent = null)
    {
        var activity = new AccountActivity
        {
            AccountId = accountId,
            ActivityType = ActivityType.Visit,
            Description = "账号访问",
            ActivityTime = DateTime.Now,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _context.Fsql.Insert(activity).ExecuteAffrowsAsync();

        // 更新账号最后访问时间
        await _context.Fsql.Update<Account>()
            .Set(a => a.LastVisited, DateTime.Now)
            .Where(a => a.Id == accountId)
            .ExecuteAffrowsAsync();
    }

    public async Task<List<AccountActivity>> GetAccountActivitiesAsync(Guid accountId)
    {
        return await _context.Fsql.Select<AccountActivity>()
            .Where(aa => aa.AccountId == accountId)
            .OrderByDescending(aa => aa.ActivityTime)
            .ToListAsync();
    }

    public async Task<byte[]> ExportAccountsCsvAsync()
    {
        var accounts = await _context.Fsql.Select<Account>()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        // Header
        sb.AppendLine("Name,Username,Password,Url,Notes,Tags,Category,ExpireAt,IsActive,ReminderCycle,ReminderType,AuthenticatorKey");

        foreach (var a in accounts)
        {
            string Escape(string? value)
            {
                value ??= string.Empty;
                if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
                {
                    return "\"" + value.Replace("\"", "\"\"") + "\"";
                }
                return value;
            }

            var decryptedPassword = _encryptionService.Decrypt(a.Password ?? string.Empty);
            var decryptedKey = string.IsNullOrEmpty(a.AuthenticatorKey) ? string.Empty : _encryptionService.Decrypt(a.AuthenticatorKey);

            var line = string.Join(',', new[]
            {
                Escape(a.Name),
                Escape(a.Username ?? string.Empty),
                Escape(decryptedPassword),
                Escape(a.Url ?? string.Empty),
                Escape(a.Notes ?? string.Empty),
                Escape(a.Tags ?? string.Empty),
                Escape(a.Category ?? string.Empty),
                Escape(a.ExpireAt?.ToString("yyyy-MM-dd") ?? string.Empty),
                a.IsActive ? "true" : "false",
                a.ReminderCycle.ToString(),
                ((int)a.ReminderType).ToString(),
                Escape(decryptedKey)
            });

            sb.AppendLine(line);
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<(int imported, int skipped)> ImportAccountsCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);
        string? headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
        {
            return (0, 0);
        }

        // Parse header
        var headers = ParseCsvLine(headerLine);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            headerIndex[headers[i]] = i;
        }

        int GetIndex(string name) => headerIndex.TryGetValue(name, out var idx) ? idx : -1;

        int imported = 0, skipped = 0;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = ParseCsvLine(line);
            string Get(string col)
            {
                var i = GetIndex(col);
                return i >= 0 && i < cols.Count ? cols[i] : string.Empty;
            }

            var username = Get("Username");
            if (string.IsNullOrWhiteSpace(username))
            {
                skipped++;
                continue;
            }

            var name = Get("Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = username;
            }

            var password = Get("Password");
            var url = Get("Url");
            var notes = Get("Notes");
            var tags = Get("Tags");
            var category = Get("Category");
            var expireAtStr = Get("ExpireAt");
            var isActiveStr = Get("IsActive");
            var reminderCycleStr = Get("ReminderCycle");
            var reminderTypeStr = Get("ReminderType");
            var authenticatorKey = Get("AuthenticatorKey");

            DateTime? expireAt = null;
            if (DateTime.TryParse(expireAtStr, out var dt)) expireAt = dt;
            bool isActive = true;
            if (bool.TryParse(isActiveStr, out var b)) isActive = b;
            int reminderCycle = 30;
            if (int.TryParse(reminderCycleStr, out var rc)) reminderCycle = rc;
            ReminderType reminderType = ReminderType.Custom;
            if (int.TryParse(reminderTypeStr, out var rtInt) && Enum.IsDefined(typeof(ReminderType), rtInt))
            {
                reminderType = (ReminderType)rtInt;
            }

            var account = new Account
            {
                Name = name,
                Username = username,
                Password = password,
                Url = string.IsNullOrWhiteSpace(url) ? null : url,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                Tags = string.IsNullOrWhiteSpace(tags) ? null : tags,
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                ExpireAt = expireAt,
                IsActive = isActive,
                ReminderCycle = reminderCycle,
                ReminderType = reminderType,
                AuthenticatorKey = string.IsNullOrWhiteSpace(authenticatorKey) ? null : authenticatorKey
            };

            await CreateAccountAsync(account);
            imported++;

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return (imported, skipped);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(line)) return result;

        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        result.Add(current.ToString());
        return result;
    }

    public async Task<byte[]> ExportAccountsExcelAsync()
    {
        var accounts = await _context.Fsql.Select<Account>()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Accounts");
        string[] headers = new[] { "Name", "Username", "Password", "Url", "Notes", "Tags", "Category", "ExpireAt", "IsActive", "ReminderCycle", "ReminderType", "AuthenticatorKey" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var a in accounts)
        {
            ws.Cell(row, 1).Value = a.Name;
            ws.Cell(row, 2).Value = a.Username ?? string.Empty;
            ws.Cell(row, 3).Value = _encryptionService.Decrypt(a.Password ?? string.Empty);
            ws.Cell(row, 4).Value = a.Url ?? string.Empty;
            ws.Cell(row, 5).Value = a.Notes ?? string.Empty;
            ws.Cell(row, 6).Value = a.Tags ?? string.Empty;
            ws.Cell(row, 7).Value = a.Category ?? string.Empty;
            ws.Cell(row, 8).Value = a.ExpireAt?.ToString("yyyy-MM-dd") ?? string.Empty;
            ws.Cell(row, 9).Value = a.IsActive;
            ws.Cell(row, 10).Value = a.ReminderCycle;
            ws.Cell(row, 11).Value = (int)a.ReminderType;
            ws.Cell(row, 12).Value = string.IsNullOrEmpty(a.AuthenticatorKey) ? string.Empty : _encryptionService.Decrypt(a.AuthenticatorKey);
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<(int imported, int skipped)> ImportAccountsExcelAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook(excelStream);
        var ws = wb.Worksheets.Worksheet(1);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumn = ws.LastColumnUsed().ColumnNumber();
        for (int c = 1; c <= lastColumn; c++)
        {
            headerIndex[ws.Cell(1, c).GetString()] = c;
        }

        int GetIndex(string name) => headerIndex.TryGetValue(name, out var idx) ? idx : -1;
        string Get(int row, string col)
        {
            var i = GetIndex(col);
            return i > 0 ? ws.Cell(row, i).GetString() : string.Empty;
        }

        int imported = 0, skipped = 0;
        var lastRow = ws.LastRowUsed().RowNumber();
        for (int r = 2; r <= lastRow; r++)
        {
            var username = Get(r, "Username");
            if (string.IsNullOrWhiteSpace(username)) { skipped++; continue; }

            var name = Get(r, "Name");
            if (string.IsNullOrWhiteSpace(name)) name = username;

            var password = Get(r, "Password");
            var url = Get(r, "Url");
            var notes = Get(r, "Notes");
            var tags = Get(r, "Tags");
            var category = Get(r, "Category");
            var expireAtStr = Get(r, "ExpireAt");
            var isActiveStr = Get(r, "IsActive");
            var reminderCycleStr = Get(r, "ReminderCycle");
            var reminderTypeStr = Get(r, "ReminderType");
            var authenticatorKey = Get(r, "AuthenticatorKey");

            DateTime? expireAt = null; if (DateTime.TryParse(expireAtStr, out var dt)) expireAt = dt;
            bool isActive = true; if (bool.TryParse(isActiveStr, out var b)) isActive = b;
            int reminderCycle = 30; if (int.TryParse(reminderCycleStr, out var rc)) reminderCycle = rc;
            ReminderType reminderType = ReminderType.Custom; if (int.TryParse(reminderTypeStr, out var rtInt) && Enum.IsDefined(typeof(ReminderType), rtInt)) reminderType = (ReminderType)rtInt;

            var account = new Account
            {
                Name = name,
                Username = username,
                Password = password,
                Url = string.IsNullOrWhiteSpace(url) ? null : url,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                Tags = string.IsNullOrWhiteSpace(tags) ? null : tags,
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                ExpireAt = expireAt,
                IsActive = isActive,
                ReminderCycle = reminderCycle,
                ReminderType = reminderType,
                AuthenticatorKey = string.IsNullOrWhiteSpace(authenticatorKey) ? null : authenticatorKey
            };

            await CreateAccountAsync(account);
            imported++;

            if (cancellationToken.IsCancellationRequested) break;
        }

        return (imported, skipped);
    }

    public Task<byte[]> GenerateAccountsExcelTemplateAsync()
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Template");
        string[] headers = new[] { "Name", "Username", "Password", "Url", "Notes", "Tags", "Category", "ExpireAt", "IsActive", "ReminderCycle", "ReminderType", "AuthenticatorKey" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Cell(2, 2).Value = "必填";
        ws.Cell(2, 1).Value = "可留空，留空则使用Username";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
