using FreeSql;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 提醒记录服务实现
/// </summary>
public class ReminderRecordService : IReminderRecordService
{
    private readonly ILogger<ReminderRecordService> _logger;
    private readonly IFreeSql _freeSql;

    public ReminderRecordService(ILogger<ReminderRecordService> logger, IFreeSql freeSql)
    {
        _logger = logger;
        _freeSql = freeSql;
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            // 自动创建表
            _freeSql.CodeFirst.SyncStructure<ReminderRecord>();
            _logger.LogInformation("提醒记录数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化提醒记录数据库失败");
        }
    }

    /// <summary>
    /// 添加提醒记录
    /// </summary>
    public async Task<bool> AddRecordAsync(ReminderRecord record)
    {
        try
        {
            var result = await _freeSql.Insert(record).ExecuteAffrowsAsync();
            _logger.LogInformation("提醒记录添加成功: {RecordType} - {AccountName}", record.RecordType, record.AccountName);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加提醒记录失败");
            return false;
        }
    }

    /// <summary>
    /// 获取提醒记录列表
    /// </summary>
    public async Task<List<ReminderRecord>> GetRecordsAsync(int page = 1, int pageSize = 20, string? recordType = null, string? status = null)
    {
        try
        {
            var query = _freeSql.Select<ReminderRecord>();

            if (!string.IsNullOrEmpty(recordType))
            {
                query = query.Where(r => r.RecordType == recordType);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var records = await query
                .OrderByDescending(r => r.CreatedAt)
                .Page(page, pageSize)
                .ToListAsync();

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提醒记录失败");
            return new List<ReminderRecord>();
        }
    }

    /// <summary>
    /// 获取记录总数
    /// </summary>
    public async Task<int> GetRecordsCountAsync(string? recordType = null, string? status = null)
    {
        try
        {
            var query = _freeSql.Select<ReminderRecord>();

            if (!string.IsNullOrEmpty(recordType))
            {
                query = query.Where(r => r.RecordType == recordType);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var count = await query.CountAsync();
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提醒记录总数失败");
            return 0;
        }
    }

    /// <summary>
    /// 删除记录
    /// </summary>
    public async Task<bool> DeleteRecordAsync(Guid id)
    {
        try
        {
            var result = await _freeSql.Delete<ReminderRecord>().Where(r => r.Id == id).ExecuteAffrowsAsync();
            _logger.LogInformation("删除提醒记录: {Id}, 影响行数: {Result}", id, result);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除提醒记录失败: {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// 清空所有记录
    /// </summary>
    public async Task<bool> ClearAllRecordsAsync()
    {
        try
        {
            var result = await _freeSql.Delete<ReminderRecord>().ExecuteAffrowsAsync();
            _logger.LogInformation("清空所有提醒记录，影响行数: {Result}", result);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空提醒记录失败");
            return false;
        }
    }
}
