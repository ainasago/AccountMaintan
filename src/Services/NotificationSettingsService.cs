using System.Text.Json;
using WebUI.Models;

namespace WebUI.Services;

/// <summary>
/// 通知设置服务实现
/// </summary>
public class NotificationSettingsService : INotificationSettingsService
{
    private readonly ILogger<NotificationSettingsService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly string _settingsFilePath;
    private readonly IReminderRecordService _recordService;

    public NotificationSettingsService(
        ILogger<NotificationSettingsService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IReminderRecordService recordService)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        _recordService = recordService;
        _settingsFilePath = Path.Combine(_environment.ContentRootPath, "Data", "notification-settings.json");
    }

    /// <summary>
    /// 获取通知设置
    /// </summary>
    public async Task<NotificationSettings> GetSettingsAsync()
    {
        try
        {
            // 从 appsettings.json 读取提醒配置（仅作为默认值）
            var reminderSection = _configuration.GetSection("Reminder");
            var enableAutoReminder = reminderSection.GetValue<bool>("EnableAutoReminder", true);
            var defaultCheckInterval = reminderSection.GetValue<string>("CheckInterval", "0 9 * * *");

            // 从文件读取其他设置
            NotificationSettings settings;
            if (!File.Exists(_settingsFilePath))
            {
                // 返回默认设置
                _logger.LogInformation("设置文件不存在，创建默认设置");
                settings = new NotificationSettings();
                // 使用配置文件中的默认值
                settings.Reminder.EnableAutoReminder = enableAutoReminder;
                settings.Reminder.CheckInterval = defaultCheckInterval;
                await SaveSettingsAsync(settings);
            }
            else
            {
                _logger.LogInformation("从文件读取设置: {FilePath}", _settingsFilePath);
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                settings = JsonSerializer.Deserialize<NotificationSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new NotificationSettings();
                
                _logger.LogInformation("从文件读取的设置: CheckInterval={CheckInterval}, DefaultReminderCycle={DefaultReminderCycle}, ReminderHour={ReminderHour}, ReminderMinute={ReminderMinute}", 
                    settings.Reminder.CheckInterval, 
                    settings.Reminder.DefaultReminderCycle, 
                    settings.Reminder.ReminderHour, 
                    settings.Reminder.ReminderMinute);
                
                // 如果文件中的设置为空，使用配置文件中的默认值
                if (string.IsNullOrEmpty(settings.Reminder.CheckInterval))
                {
                    _logger.LogInformation("文件中的CheckInterval为空，使用默认值: {DefaultValue}", defaultCheckInterval);
                    settings.Reminder.CheckInterval = defaultCheckInterval;
                }
                // EnableAutoReminder 是 bool 类型，不需要检查 HasValue
            }

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通知设置失败");
            return new NotificationSettings();
        }
    }

    /// <summary>
    /// 保存通知设置
    /// </summary>
    public async Task<bool> SaveSettingsAsync(NotificationSettings settings)
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 添加调试日志
            _logger.LogInformation("准备保存设置: CheckInterval={CheckInterval}, DefaultReminderCycle={DefaultReminderCycle}, ReminderHour={ReminderHour}, ReminderMinute={ReminderMinute}", 
                settings.Reminder.CheckInterval, 
                settings.Reminder.DefaultReminderCycle, 
                settings.Reminder.ReminderHour, 
                settings.Reminder.ReminderMinute);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger.LogInformation("通知设置保存成功到文件: {FilePath}", _settingsFilePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存通知设置失败");
            return false;
        }
    }

    /// <summary>
    /// 测试邮件配置
    /// </summary>
    public async Task<bool> TestEmailSettingsAsync(EmailSettings settings)
    {
        try
        {
            if (!settings.IsEnabled)
            {
                _logger.LogWarning("邮件通知未启用");
                return false;
            }

            var recipients = settings.ToEmails?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList() ?? new();
            if (recipients.Count == 0)
            {
                _logger.LogWarning("缺少收件人");
                return false;
            }

            if (settings.UseGmailApi)
            {
                // 优先使用提供的 OAuth 三件套（含 refresh_token），否则走 credentials.json/token.json 交互授权模式
                if (!string.IsNullOrWhiteSpace(settings.GmailClientId) &&
                    !string.IsNullOrWhiteSpace(settings.GmailClientSecret) &&
                    !string.IsNullOrWhiteSpace(settings.GmailRefreshToken))
                {
                    return await SendByGmailApiAsync(settings, recipients);
                }
                else
                {
                    return await SendByGmailApiWithLocalCredentialsAsync(settings, recipients);
                }
            }
            else
            {
                // 回退 SMTP 发送（保留）
                if (string.IsNullOrWhiteSpace(settings.SmtpServer) || string.IsNullOrWhiteSpace(settings.FromEmail))
                {
                    _logger.LogWarning("SMTP 配置不完整");
                    return false;
                }

                using var message = new System.Net.Mail.MailMessage()
                {
                    From = new System.Net.Mail.MailAddress(settings.FromEmail, string.IsNullOrWhiteSpace(settings.FromName) ? settings.FromEmail : settings.FromName),
                    Subject = "账号维护系统 - 测试邮件",
                    Body = $"这是一封测试邮件。发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    IsBodyHtml = false
                };
                foreach (var to in recipients)
                {
                    message.To.Add(to);
                }

                using var smtp = new System.Net.Mail.SmtpClient(settings.SmtpServer, settings.SmtpPort)
                {
                    EnableSsl = settings.UseSsl,
                    DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(settings.Username ?? settings.FromEmail, settings.Password ?? string.Empty)
                };

                await smtp.SendMailAsync(message);
                _logger.LogInformation("测试邮件发送成功，收件人: {Recipients}", string.Join(",", recipients));
                
                // 记录测试历史
                await RecordNotificationAsync("测试账号", null, "Test", "Email", true, null, "测试邮件发送成功");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件配置测试失败");
            return false;
        }
    }

    private async Task<bool> SendByGmailApiAsync(EmailSettings settings, List<string> recipients)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.GmailClientId) || string.IsNullOrWhiteSpace(settings.GmailClientSecret) || string.IsNullOrWhiteSpace(settings.GmailRefreshToken) || string.IsNullOrWhiteSpace(settings.FromEmail))
            {
                _logger.LogWarning("Gmail API 配置不完整");
                return false;
            }

            var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                RefreshToken = settings.GmailRefreshToken
            };
            var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = settings.GmailClientId,
                    ClientSecret = settings.GmailClientSecret
                },
                Scopes = new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend }
            });
            var credential = new Google.Apis.Auth.OAuth2.UserCredential(flow, settings.FromEmail, token);

            // 强制刷新获取 AccessToken
            bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!refreshed && string.IsNullOrWhiteSpace(credential.Token.AccessToken))
            {
                _logger.LogWarning("无法刷新 Gmail 访问令牌");
                return false;
            }

            using var gmail = new Google.Apis.Gmail.v1.GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AccountMaintan"
            });

            var mimeMessage = $"From: {settings.FromName} <{settings.FromEmail}>\r\n" +
                              $"To: {string.Join(", ", recipients)}\r\n" +
                              "Subject: 账号维护系统 - 测试邮件\r\n" +
                              "Content-Type: text/plain; charset=utf-8\r\n\r\n" +
                              $"这是一封通过 Gmail API 发送的测试邮件。发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            var raw = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mimeMessage))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);

            var message = new Google.Apis.Gmail.v1.Data.Message { Raw = raw };
            var request = gmail.Users.Messages.Send(message, "me");
            var result = await request.ExecuteAsync();

            _logger.LogInformation("Gmail API 测试邮件已发送，Id: {Id}", result?.Id);
            
            // 记录测试历史
            await RecordNotificationAsync("测试账号", null, "Test", "Email", !string.IsNullOrEmpty(result?.Id), null, "Gmail API 测试邮件发送成功");
            return !string.IsNullOrEmpty(result?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail API 发送失败");
            return false;
        }
    }

    private async Task<bool> SendByGmailApiWithLocalCredentialsAsync(EmailSettings settings, List<string> recipients)
    {
        try
        {
            var credCandidates = new[]
            {
                Path.Combine(_environment.ContentRootPath, "credentials.json"),
                Path.Combine(_environment.ContentRootPath, "Data", "credentials.json")
            };
            var credFile = credCandidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(credFile))
            {
                _logger.LogWarning("未找到 credentials.json，请将文件放在应用根目录或 Data 目录");
                return false;
            }

            using var stream = new FileStream(credFile, FileMode.Open, FileAccess.Read);

            var credPath = Path.Combine(_environment.ContentRootPath, "token.json");
            var credential = await Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                Google.Apis.Auth.OAuth2.GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend },
                "user",
                CancellationToken.None,
                new Google.Apis.Util.Store.FileDataStore(credPath, true));

            using var gmail = new Google.Apis.Gmail.v1.GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AccountMaintan"
            });

            var mime = new MimeKit.MimeMessage();
            mime.From.Add(new MimeKit.MailboxAddress(settings.FromName ?? settings.FromEmail, settings.FromEmail));
            foreach (var to in recipients)
                mime.To.Add(new MimeKit.MailboxAddress(to, to));
            mime.Subject = "账号维护系统 - 测试邮件";
            mime.Body = new MimeKit.TextPart("plain") { Text = $"这是一封通过 Gmail API 发送的测试邮件。发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" };

            using var ms = new MemoryStream();
            mime.WriteTo(ms);
            var raw = Convert.ToBase64String(ms.ToArray()).Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);

            var message = new Google.Apis.Gmail.v1.Data.Message { Raw = raw };
            var result = await gmail.Users.Messages.Send(message, "me").ExecuteAsync();
            _logger.LogInformation("Gmail API (credentials.json) 测试邮件已发送，Id: {Id}", result?.Id);
            
            // 记录测试历史
            await RecordNotificationAsync("测试账号", null, "Test", "Email", !string.IsNullOrEmpty(result?.Id), null, "Gmail API (credentials.json) 测试邮件发送成功");
            return !string.IsNullOrEmpty(result?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail API (credentials.json) 发送失败");
            return false;
        }
    }

    private async Task<bool> SendByGmailApiWithCustomContentAsync(EmailSettings settings, List<string> recipients, string subject, string body, string? userId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.GmailClientId) || string.IsNullOrWhiteSpace(settings.GmailClientSecret) || string.IsNullOrWhiteSpace(settings.GmailRefreshToken) || string.IsNullOrWhiteSpace(settings.FromEmail))
            {
                _logger.LogWarning("Gmail API 配置不完整");
                return false;
            }

            var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                RefreshToken = settings.GmailRefreshToken
            };
            var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = settings.GmailClientId,
                    ClientSecret = settings.GmailClientSecret
                },
                Scopes = new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend }
            });
            var credential = new Google.Apis.Auth.OAuth2.UserCredential(flow, settings.FromEmail, token);

            // 强制刷新获取 AccessToken
            bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!refreshed && string.IsNullOrWhiteSpace(credential.Token.AccessToken))
            {
                _logger.LogWarning("无法刷新 Gmail 访问令牌");
                return false;
            }

            using var gmail = new Google.Apis.Gmail.v1.GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AccountMaintan"
            });

            var mime = new MimeKit.MimeMessage();
            mime.From.Add(new MimeKit.MailboxAddress(settings.FromName ?? settings.FromEmail, settings.FromEmail));
            foreach (var to in recipients)
                mime.To.Add(new MimeKit.MailboxAddress(to, to));
            mime.Subject = subject;
            mime.Body = new MimeKit.TextPart("plain") { Text = body };

            using var ms = new MemoryStream();
            mime.WriteTo(ms);
            var raw = Convert.ToBase64String(ms.ToArray()).Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);

            var message = new Google.Apis.Gmail.v1.Data.Message { Raw = raw };
            var result = await gmail.Users.Messages.Send(message, "me").ExecuteAsync();
            _logger.LogInformation("Gmail API 邮件已发送，Id: {Id}", result?.Id);
            
            // 记录发送历史
            await RecordNotificationAsync("实际账号", null, "Reminder", "Email", !string.IsNullOrEmpty(result?.Id), userId, "Gmail API 邮件发送成功");
            return !string.IsNullOrEmpty(result?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail API 发送失败");
            return false;
        }
    }

    private async Task<bool> SendByGmailApiWithLocalCredentialsAndCustomContentAsync(EmailSettings settings, List<string> recipients, string subject, string body, string? userId = null)
    {
        try
        {
            var credCandidates = new[]
            {
                Path.Combine(_environment.ContentRootPath, "credentials.json"),
                Path.Combine(_environment.ContentRootPath, "Data", "credentials.json")
            };
            var credFile = credCandidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(credFile))
            {
                _logger.LogWarning("未找到 credentials.json，请将文件放在应用根目录或 Data 目录");
                return false;
            }

            using var stream = new FileStream(credFile, FileMode.Open, FileAccess.Read);

            var credPath = Path.Combine(_environment.ContentRootPath, "token.json");
            var credential = await Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                Google.Apis.Auth.OAuth2.GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend },
                "user",
                CancellationToken.None,
                new Google.Apis.Util.Store.FileDataStore(credPath, true));

            using var gmail = new Google.Apis.Gmail.v1.GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AccountMaintan"
            });

            var mime = new MimeKit.MimeMessage();
            mime.From.Add(new MimeKit.MailboxAddress(settings.FromName ?? settings.FromEmail, settings.FromEmail));
            foreach (var to in recipients)
                mime.To.Add(new MimeKit.MailboxAddress(to, to));
            mime.Subject = subject;
            mime.Body = new MimeKit.TextPart("plain") { Text = body };

            using var ms = new MemoryStream();
            mime.WriteTo(ms);
            var raw = Convert.ToBase64String(ms.ToArray()).Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);

            var message = new Google.Apis.Gmail.v1.Data.Message { Raw = raw };
            var result = await gmail.Users.Messages.Send(message, "me").ExecuteAsync();
            _logger.LogInformation("Gmail API (credentials.json) 邮件已发送，Id: {Id}", result?.Id);
            
            // 记录发送历史
            await RecordNotificationAsync("实际账号", null, "Reminder", "Email", !string.IsNullOrEmpty(result?.Id), userId, "Gmail API (credentials.json) 邮件发送成功");
            return !string.IsNullOrEmpty(result?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail API (credentials.json) 发送失败");
            return false;
        }
    }

    /// <summary>
    /// 测试Telegram配置
    /// </summary>
    public async Task<bool> TestTelegramSettingsAsync(TelegramSettings settings)
    {
        try
        {
            if (!settings.IsEnabled)
            {
                _logger.LogWarning("Telegram通知未启用");
                return false;
            }

            if (string.IsNullOrEmpty(settings.BotToken) || string.IsNullOrEmpty(settings.ChatId))
            {
                _logger.LogWarning("Telegram配置不完整");
                return false;
            }

            using var http = new HttpClient();
            var apiUrl = $"https://api.telegram.org/bot{settings.BotToken}/sendMessage";

            var payload = new
            {
                chat_id = settings.ChatId,
                text = $"账号维护系统 测试消息\n时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                parse_mode = settings.EnableMarkdown ? "Markdown" : (string?)null,
                disable_web_page_preview = true
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
            {
                IgnoreNullValues = true
            });

            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await http.PostAsync(apiUrl, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telegram 发送失败: {Status} {Body}", resp.StatusCode, respText);
                return false;
            }

            _logger.LogInformation("Telegram 测试消息发送成功");
            
            // 记录测试历史
            await RecordNotificationAsync("测试账号", null, "Test", "Telegram", true, null, "Telegram 测试消息发送成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram配置测试失败");
            return false;
        }
    }

    /// <summary>
    /// 发送测试通知
    /// </summary>
    public async Task<bool> SendTestNotificationAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            var success = true;

            // 测试邮件通知
            if (settings.Email.IsEnabled)
            {
                success &= await TestEmailSettingsAsync(settings.Email);
            }

            // 测试Telegram通知
            if (settings.Telegram.IsEnabled)
            {
                success &= await TestTelegramSettingsAsync(settings.Telegram);
            }

            // SignalR通知总是可用的
            _logger.LogInformation("SignalR通知测试成功");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送测试通知失败");
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string subject, string body, string? userId = null)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Email.IsEnabled)
        {
            _logger.LogInformation("邮件未启用");
            return false;
        }

        var recipients = settings.Email.ToEmails?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList() ?? new();
        if (recipients.Count == 0)
        {
            _logger.LogWarning("缺少收件人");
            return false;
        }

        if (settings.Email.UseGmailApi)
        {
            // 优先使用提供的 OAuth 三件套（含 refresh_token），否则走 credentials.json/token.json 交互授权模式
            if (!string.IsNullOrWhiteSpace(settings.Email.GmailClientId) &&
                !string.IsNullOrWhiteSpace(settings.Email.GmailClientSecret) &&
                !string.IsNullOrWhiteSpace(settings.Email.GmailRefreshToken))
            {
                return await SendByGmailApiWithCustomContentAsync(settings.Email, recipients, subject, body, userId);
            }
            else
            {
                return await SendByGmailApiWithLocalCredentialsAndCustomContentAsync(settings.Email, recipients, subject, body, userId);
            }
        }
        else
        {
            // 回退 SMTP 发送（保留）
            if (string.IsNullOrWhiteSpace(settings.Email.SmtpServer) || string.IsNullOrWhiteSpace(settings.Email.FromEmail))
            {
                _logger.LogWarning("SMTP 配置不完整");
                return false;
            }

            using var message = new System.Net.Mail.MailMessage()
            {
                From = new System.Net.Mail.MailAddress(settings.Email.FromEmail, string.IsNullOrWhiteSpace(settings.Email.FromName) ? settings.Email.FromEmail : settings.Email.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            foreach (var to in recipients)
            {
                message.To.Add(to);
            }

            using var smtp = new System.Net.Mail.SmtpClient(settings.Email.SmtpServer, settings.Email.SmtpPort)
            {
                EnableSsl = settings.Email.UseSsl,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(settings.Email.Username ?? settings.Email.FromEmail, settings.Email.Password ?? string.Empty)
            };

            await smtp.SendMailAsync(message);
            _logger.LogInformation("邮件发送成功，收件人: {Recipients}", string.Join(",", recipients));
            
            // 记录发送历史
            await RecordNotificationAsync("实际账号", null, "Reminder", "Email", true, userId, "邮件发送成功");
            return true;
        }
    }

    public async Task<bool> SendTelegramAsync(string text, bool enableMarkdown = false, string? userId = null)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Telegram.IsEnabled)
        {
            _logger.LogInformation("Telegram 未启用");
            return false;
        }

        try
        {
            using var http = new HttpClient();
            var apiUrl = $"https://api.telegram.org/bot{settings.Telegram.BotToken}/sendMessage";
            var payload = new
            {
                chat_id = settings.Telegram.ChatId,
                text = text,
                parse_mode = enableMarkdown ? "Markdown" : (string?)null,
                disable_web_page_preview = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { IgnoreNullValues = true });
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await http.PostAsync(apiUrl, content);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("Telegram 发送失败: {Status} {Body}", resp.StatusCode, body);
                
                // 记录发送失败历史
                await RecordNotificationAsync("实际账号", null, "Reminder", "Telegram", false, userId, null, $"HTTP {resp.StatusCode}: {body}");
                return false;
            }
            
            // 记录发送成功历史
            await RecordNotificationAsync("实际账号", null, "Reminder", "Telegram", true, userId, "Telegram 消息发送成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram 发送异常");
            return false;
        }
    }

    /// <summary>
    /// 替换模板中的变量
    /// </summary>
    private string ReplaceVariables(string template, string accountName = "", string accountId = "")
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return template
            .Replace("{AccountName}", accountName)
            .Replace("{AccountId}", accountId)
            .Replace("{Now}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    /// <summary>
    /// 记录提醒历史
    /// </summary>
    private async Task RecordNotificationAsync(string accountName, string? accountId, string recordType, string channel, bool success, string? userId = null, string? message = null, string? errorMessage = null)
    {
        try
        {
            var record = new ReminderRecord
            {
                AccountName = accountName,
                AccountId = !string.IsNullOrEmpty(accountId) ? Guid.Parse(accountId) : null,
                UserId = userId ?? string.Empty,
                RecordType = recordType,
                NotificationChannel = channel,
                Status = success ? "Success" : "Failed",
                Message = message,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.Now,
                SentAt = success ? DateTime.Now : null
            };

            await _recordService.AddRecordAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录提醒历史失败");
        }
    }

    /// <summary>
    /// 使用模板发送邮件
    /// </summary>
    public async Task<bool> SendEmailWithTemplateAsync(string accountName = "", string accountId = "", string? userId = null)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Email.IsEnabled)
        {
            _logger.LogInformation("邮件未启用");
            return false;
        }

        // 使用模板生成主题和正文
        var subject = ReplaceVariables(settings.Email.SubjectTemplate ?? "账号提醒 - {AccountName}", accountName, accountId);
        var body = ReplaceVariables(settings.Email.BodyTemplate ?? "账号 '{AccountName}' 需要访问，请及时登录。时间: {Now}", accountName, accountId);

        var success = await SendEmailAsync(subject, body);
        
        // 记录实际账号提醒历史
        await RecordNotificationAsync(accountName, accountId, "Reminder", "Email", success, userId, success ? "邮件提醒发送成功" : "邮件提醒发送失败");
        
        return success;
    }

    /// <summary>
    /// 使用模板发送Telegram消息
    /// </summary>
    public async Task<bool> SendTelegramWithTemplateAsync(string accountName = "", string accountId = "", string? userId = null)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Telegram.IsEnabled)
        {
            _logger.LogInformation("Telegram 未启用");
            return false;
        }

        // 使用模板生成消息内容
        var message = ReplaceVariables(settings.Telegram.TextTemplate ?? "*账号提醒*\n账号: `{AccountName}`\n时间: {Now}", accountName, accountId);

        var success = await SendTelegramAsync(message, settings.Telegram.EnableMarkdown);
        
        // 记录实际账号提醒历史
        await RecordNotificationAsync(accountName, accountId, "Reminder", "Telegram", success, userId, success ? "Telegram提醒发送成功" : "Telegram提醒发送失败");
        
        return success;
    }
}
