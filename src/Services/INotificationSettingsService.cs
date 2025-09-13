using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 通知设置服务接口
/// </summary>
public interface INotificationSettingsService
{
    /// <summary>
    /// 获取通知设置
    /// </summary>
    Task<NotificationSettings> GetSettingsAsync();

    /// <summary>
    /// 保存通知设置
    /// </summary>
    Task<bool> SaveSettingsAsync(NotificationSettings settings);

    /// <summary>
    /// 测试邮件配置
    /// </summary>
    Task<bool> TestEmailSettingsAsync(EmailSettings settings);

    /// <summary>
    /// 测试Telegram配置
    /// </summary>
    Task<bool> TestTelegramSettingsAsync(TelegramSettings settings);

    /// <summary>
    /// 发送邮件（根据当前保存的设置）
    /// </summary>
    Task<bool> SendEmailAsync(string subject, string body);

    /// <summary>
    /// 发送 Telegram 消息（根据当前保存的设置）
    /// </summary>
    Task<bool> SendTelegramAsync(string text, bool enableMarkdown = false);

    /// <summary>
    /// 发送测试通知
    /// </summary>
    Task<bool> SendTestNotificationAsync();

    /// <summary>
    /// 使用模板发送邮件
    /// </summary>
    Task<bool> SendEmailWithTemplateAsync(string accountName = "", string accountId = "");

    /// <summary>
    /// 使用模板发送Telegram消息
    /// </summary>
    Task<bool> SendTelegramWithTemplateAsync(string accountName = "", string accountId = "");
}
