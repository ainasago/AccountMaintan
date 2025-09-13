using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// 账号API控制器
/// </summary>
[Authorize]
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IEncryptionService _encryptionService;
    private readonly ITotpService _totpService;
    private readonly IAdminService _adminService;

    public AccountsController(IAccountService accountService, 
                            IEncryptionService encryptionService,
                            ITotpService totpService,
                            IAdminService adminService)
    {
        _accountService = accountService;
        _encryptionService = encryptionService;
        _totpService = totpService;
        _adminService = adminService;
    }

    /// <summary>
    /// 获取所有账号
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(new { success = false, message = "用户未认证" });
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            List<Account> accounts;
            
            if (isAdmin)
            {
                // 管理员可以看到所有账号
                accounts = await _accountService.GetAllAccountsAsync();
            }
            else
            {
                // 普通用户只能看到自己的账号
                accounts = await _accountService.GetUserAccountsAsync(currentUserId);
            }

            return Ok(new { success = true, data = accounts });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "获取账号列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取账号
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(new { success = false, message = "用户未认证" });
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            Account? account;
            
            if (isAdmin)
            {
                // 管理员可以查看任何账号
                account = await _accountService.GetAccountByIdAsync(id);
            }
            else
            {
                // 普通用户只能查看自己的账号
                account = await _accountService.GetUserAccountByIdAsync(id, currentUserId);
            }

            if (account == null)
            {
                return NotFound(new { success = false, message = "账号不存在" });
            }

            // 先处理安全问题
            var securityQuestions = account.SecurityQuestions.Select(sq => new
            {
                sq.Id,
                sq.Question,
                Answer = _encryptionService.Decrypt(sq.Answer),
                sq.CreatedAt
            }).ToList();

            var displayAccount = new
            {
                account.Id,
                account.Name,
                account.Url,
                Username = account.Username ?? string.Empty,
                account.Category,
                account.Tags,
                account.Notes,
                account.IsActive,
                account.CreatedAt,
                account.LastVisited,
                account.ReminderCycle,
                account.ReminderType,
                SecurityQuestions = securityQuestions,
                account.Activities
            };

            return Ok(new { success = true, data = displayAccount });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "获取账号失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取账号密码（解密后）
    /// </summary>
    [HttpGet("{id}/password")]
    public async Task<IActionResult> GetAccountPassword(Guid id)
    {
        try
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound(new { success = false, message = "账号不存在" });
            }

            var decryptedPassword = _encryptionService.Decrypt(account.Password ?? string.Empty);
            
            return Ok(new { 
                success = true, 
                username = account.Username ?? string.Empty,
                password = decryptedPassword 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "获取密码失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 创建新账号
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] Account account)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(new { success = false, message = "用户未认证" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "数据验证失败", errors = ModelState });
            }

            // 设置用户ID
            account.UserId = currentUserId;

            var createdAccount = await _accountService.CreateAccountAsync(account);
            return CreatedAtAction(nameof(GetAccount), new { id = createdAccount.Id }, 
                new { success = true, data = createdAccount, message = "账号创建成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "创建账号失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新账号
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] Account account)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(new { success = false, message = "用户未认证" });
            }

            if (id != account.Id)
            {
                return BadRequest(new { success = false, message = "ID不匹配" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "数据验证失败", errors = ModelState });
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            if (!isAdmin)
            {
                // 普通用户只能更新自己的账号
                var existingAccount = await _accountService.GetUserAccountByIdAsync(id, currentUserId);
                if (existingAccount == null)
                {
                    return NotFound(new { success = false, message = "账号不存在或无权访问" });
                }
            }

            // 设置用户ID（确保数据一致性）
            account.UserId = currentUserId;

            var result = await _accountService.UpdateAccountAsync(account);
            if (!result)
            {
                return NotFound(new { success = false, message = "账号不存在或更新失败" });
            }

            return Ok(new { success = true, message = "账号更新成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "更新账号失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除账号
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(new { success = false, message = "用户未认证" });
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            if (!isAdmin)
            {
                // 普通用户只能删除自己的账号
                var existingAccount = await _accountService.GetUserAccountByIdAsync(id, currentUserId);
                if (existingAccount == null)
                {
                    return NotFound(new { success = false, message = "账号不存在或无权访问" });
                }
            }

            var result = await _accountService.DeleteAccountAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "账号不存在或删除失败" });
            }

            return Ok(new { success = true, message = "账号删除成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "删除账号失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索账号
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchAccounts([FromQuery] string? keyword, 
                                                   [FromQuery] string? category, 
                                                   [FromQuery] string? tags)
    {
        try
        {
            var accounts = await _accountService.SearchAccountsAsync(keyword ?? "", category, tags);
            return Ok(new { success = true, data = accounts });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "搜索账号失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 记录账号访问
    /// </summary>
    [HttpPost("{id}/visit")]
    public async Task<IActionResult> RecordVisit(Guid id)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            await _accountService.RecordAccountVisitAsync(id, ipAddress, userAgent);
            
            return Ok(new { success = true, message = "访问记录成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "记录访问失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 生成TOTP验证码
    /// </summary>
    [HttpGet("{id}/totp")]
    public async Task<IActionResult> GenerateTOTP(Guid id)
    {
        try
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound(new { success = false, message = "账号不存在" });
            }

            if (string.IsNullOrEmpty(account.AuthenticatorKey))
            {
                return BadRequest(new { success = false, message = "该账号未配置TOTP密钥" });
            }

            var decryptedKey = _encryptionService.Decrypt(account.AuthenticatorKey);
            var totpCode = _totpService.GenerateTotp(decryptedKey);
            var qrCodeUrl = _totpService.GenerateQrCode(account.Name, decryptedKey, "AccountManager");

            return Ok(new { 
                success = true, 
                totpCode = totpCode,
                qrCodeUrl = qrCodeUrl,
                message = "TOTP验证码生成成功" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "生成TOTP失败", error = ex.Message });
        }
    }
}

/// <summary>
/// 生成TOTP请求模型
/// </summary>
public class GenerateTotpRequest
{
    public string AccountName { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AccountManager";
}

/// <summary>
/// 验证TOTP请求模型
/// </summary>
public class ValidateTotpRequest
{
    public string SecretKey { get; set; } = string.Empty;
    public string Totp { get; set; } = string.Empty;
    public int Window { get; set; } = 1;
}
