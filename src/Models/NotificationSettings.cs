namespace WebUI.Models;

/// <summary>
/// 通知设置模型
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// 邮件设置
    /// </summary>
    public EmailSettings Email { get; set; } = new();

    /// <summary>
    /// Telegram设置
    /// </summary>
    public TelegramSettings Telegram { get; set; } = new();

    /// <summary>
    /// SignalR设置
    /// </summary>
    public SignalRSettings SignalR { get; set; } = new();

    /// <summary>
    /// 提醒设置
    /// </summary>
    public ReminderSettings Reminder { get; set; } = new();
}

/// <summary>
/// 邮件设置
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// SMTP服务器地址
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP端口
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人名称
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 是否使用SSL
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// 收件人邮箱列表（逗号分隔）
    /// </summary>
    public string ToEmails { get; set; } = string.Empty;

    /// <summary>
    /// 邮件主题模板
    /// 支持变量：{AccountName} {AccountId} {Now}
    /// </summary>
    public string SubjectTemplate { get; set; } = "账号提醒 - {AccountName}";

    /// <summary>
    /// 邮件正文模板
    /// 支持变量：{AccountName} {AccountId} {Now}
    /// </summary>
    public string BodyTemplate { get; set; } = "账号 '{AccountName}' 需要访问，请及时登录。时间: {Now}";

    /// <summary>
    /// 是否使用 Gmail API 发送
    /// </summary>
    public bool UseGmailApi { get; set; } = true;

    /// <summary>
    /// Gmail OAuth 客户端 ID
    /// </summary>
    public string GmailClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gmail OAuth 客户端密钥
    /// </summary>
    public string GmailClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gmail OAuth 刷新令牌
    /// </summary>
    public string GmailRefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Telegram设置
/// </summary>
public class TelegramSettings
{
    /// <summary>
    /// 是否启用Telegram通知
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Bot Token
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// 聊天ID
    /// </summary>
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用Markdown格式
    /// </summary>
    public bool EnableMarkdown { get; set; } = true;

    /// <summary>
    /// Telegram 文本模板
    /// 支持变量：{AccountName} {AccountId} {Now}
    /// </summary>
    public string TextTemplate { get; set; } = "*账号提醒*\n账号: `{AccountName}`\n时间: {Now}";
}

/// <summary>
/// SignalR设置
/// </summary>
public class SignalRSettings
{
    /// <summary>
    /// 是否启用SignalR通知
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 通知标题
    /// </summary>
    public string NotificationTitle { get; set; } = "账号提醒";

    /// <summary>
    /// 通知图标
    /// </summary>
    public string NotificationIcon { get; set; } = "/favicon.ico";
}

/// <summary>
/// 提醒设置
/// </summary>
public class ReminderSettings
{
    /// <summary>
    /// 检查间隔（Cron表达式）
    /// </summary>
    public string CheckInterval { get; set; } = "0 * * * *";

    /// <summary>
    /// 默认提醒周期（天）
    /// </summary>
    public int DefaultReminderCycle { get; set; } = 30;

    /// <summary>
    /// 是否启用自动提醒
    /// </summary>
    public bool EnableAutoReminder { get; set; } = true;

    /// <summary>
    /// 提醒时间（小时，0-23）
    /// </summary>
    public int ReminderHour { get; set; } = 9;

    /// <summary>
    /// 提醒分钟（0-59）
    /// </summary>
    public int ReminderMinute { get; set; } = 0;
}
