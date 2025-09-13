using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.Controllers;

/// <summary>
/// 提醒记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[
    Authorize
]
public class ReminderRecordsController : ControllerBase
{
    private readonly IReminderRecordService _recordService;
    private readonly ILogger<ReminderRecordsController> _logger;
    private readonly IAdminService _adminService;

    public ReminderRecordsController(IReminderRecordService recordService, ILogger<ReminderRecordsController> logger, IAdminService adminService)
    {
        _recordService = recordService;
        _logger = logger;
        _adminService = adminService;
    }

    /// <summary>
    /// 获取提醒记录列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? recordType = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "用户未认证" });
            }

            // 检查是否为管理员
            var isAdmin = await _adminService.IsAdminAsync(currentUserId);
            
            var records = isAdmin 
                ? await _recordService.GetRecordsAsync(page, pageSize, recordType, status)
                : await _recordService.GetUserRecordsAsync(currentUserId, page, pageSize, recordType, status);
            
            var totalCount = isAdmin 
                ? await _recordService.GetRecordsCountAsync(recordType, status)
                : await _recordService.GetUserRecordsCountAsync(currentUserId, recordType, status);

            return Ok(new
            {
                records,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提醒记录失败");
            return StatusCode(500, new { message = "获取记录失败" });
        }
    }

    /// <summary>
    /// 删除单个记录
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRecord(Guid id)
    {
        try
        {
            var success = await _recordService.DeleteRecordAsync(id);
            if (success)
            {
                return Ok(new { message = "记录删除成功" });
            }
            else
            {
                return NotFound(new { message = "记录不存在" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除提醒记录失败: {Id}", id);
            return StatusCode(500, new { message = "删除记录失败" });
        }
    }

    /// <summary>
    /// 清空所有记录
    /// </summary>
    [HttpDelete("clear")]
    public async Task<ActionResult> ClearAllRecords()
    {
        try
        {
            var success = await _recordService.ClearAllRecordsAsync();
            if (success)
            {
                return Ok(new { message = "所有记录已清空" });
            }
            else
            {
                return StatusCode(500, new { message = "清空记录失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空提醒记录失败");
            return StatusCode(500, new { message = "清空记录失败" });
        }
    }

    /// <summary>
    /// 获取记录统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        try
        {
            var totalCount = await _recordService.GetRecordsCountAsync();
            var testCount = await _recordService.GetRecordsCountAsync("Test");
            var reminderCount = await _recordService.GetRecordsCountAsync("Reminder");
            var successCount = await _recordService.GetRecordsCountAsync(null, "Success");
            var failedCount = await _recordService.GetRecordsCountAsync(null, "Failed");

            return Ok(new
            {
                total = totalCount,
                test = testCount,
                reminder = reminderCount,
                success = successCount,
                failed = failedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取记录统计失败");
            return StatusCode(500, new { message = "获取统计失败" });
        }
    }
}
