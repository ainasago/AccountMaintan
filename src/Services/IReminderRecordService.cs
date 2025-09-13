using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 提醒记录服务接口
/// </summary>
public interface IReminderRecordService
{
    /// <summary>
    /// 添加提醒记录
    /// </summary>
    Task<bool> AddRecordAsync(ReminderRecord record);

    /// <summary>
    /// 获取提醒记录列表
    /// </summary>
    Task<List<ReminderRecord>> GetRecordsAsync(int page = 1, int pageSize = 20, string? recordType = null, string? status = null);

    /// <summary>
    /// 获取记录总数
    /// </summary>
    Task<int> GetRecordsCountAsync(string? recordType = null, string? status = null);

    /// <summary>
    /// 删除记录
    /// </summary>
    Task<bool> DeleteRecordAsync(Guid id);

    /// <summary>
    /// 清空所有记录
    /// </summary>
    Task<bool> ClearAllRecordsAsync();
}
