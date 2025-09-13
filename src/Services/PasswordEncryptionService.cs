using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebUI.Services;

/// <summary>
/// 密码加密服务实现
/// </summary>
public class PasswordEncryptionService : IPasswordEncryptionService
{
    private readonly ILogger<PasswordEncryptionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _encryptionKey;
    private readonly TimeSpan _tokenExpiry = TimeSpan.FromMinutes(10); // 令牌10分钟过期

    public PasswordEncryptionService(ILogger<PasswordEncryptionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // 从配置中获取加密密钥，如果没有则生成一个
        _encryptionKey = _configuration["PasswordEncryption:Key"] ?? GenerateRandomKey();
    }

    /// <summary>
    /// 生成加密令牌
    /// </summary>
    public string GenerateEncryptionToken()
    {
        try
        {
            var tokenData = new
            {
                TokenId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_tokenExpiry)
            };

            var tokenJson = JsonSerializer.Serialize(tokenData);
            var encryptedToken = EncryptString(tokenJson, _encryptionKey);
            
            _logger.LogDebug("生成密码加密令牌: {TokenId}", tokenData.TokenId);
            return encryptedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成加密令牌失败");
            throw;
        }
    }

    /// <summary>
    /// 使用令牌加密密码
    /// </summary>
    public string EncryptPassword(string password, string token)
    {
        try
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("密码不能为空", nameof(password));
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("令牌不能为空", nameof(token));
            }

            // 验证令牌
            if (!IsTokenValid(token))
            {
                throw new InvalidOperationException("令牌已过期或无效");
            }

            // 使用令牌作为额外的盐值来加密密码
            var combinedData = $"{password}:{token}";
            var encryptedPassword = EncryptString(combinedData, _encryptionKey);
            
            _logger.LogDebug("密码加密成功");
            return encryptedPassword;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码加密失败");
            throw;
        }
    }

    /// <summary>
    /// 使用令牌解密密码
    /// </summary>
    public string DecryptPassword(string encryptedPassword, string token)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedPassword))
            {
                throw new ArgumentException("加密密码不能为空", nameof(encryptedPassword));
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("令牌不能为空", nameof(token));
            }

            // 验证令牌
            if (!IsTokenValid(token))
            {
                throw new InvalidOperationException("令牌已过期或无效");
            }

            // 解密密码
            var decryptedData = DecryptString(encryptedPassword, _encryptionKey);
            var parts = decryptedData.Split(':', 2);
            
            if (parts.Length != 2)
            {
                throw new InvalidOperationException("解密数据格式无效");
            }

            var password = parts[0];
            var tokenFromData = parts[1];

            // 验证令牌是否匹配
            if (token != tokenFromData)
            {
                throw new InvalidOperationException("令牌不匹配");
            }

            _logger.LogDebug("密码解密成功");
            return password;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码解密失败");
            throw;
        }
    }

    /// <summary>
    /// 验证令牌是否有效
    /// </summary>
    public bool IsTokenValid(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var tokenJson = DecryptString(token, _encryptionKey);
            var tokenData = JsonSerializer.Deserialize<TokenData>(tokenJson);
            
            if (tokenData == null)
            {
                return false;
            }

            // 检查是否过期
            return DateTime.UtcNow <= tokenData.ExpiresAt;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取令牌过期时间
    /// </summary>
    public DateTime GetTokenExpiryTime(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return DateTime.MinValue;
            }

            var tokenJson = DecryptString(token, _encryptionKey);
            var tokenData = JsonSerializer.Deserialize<TokenData>(tokenJson);
            
            return tokenData?.ExpiresAt ?? DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// 加密字符串
    /// </summary>
    private string EncryptString(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 确保密钥长度为32字节
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);

        swEncrypt.Write(plainText);
        swEncrypt.Close();

        var encrypted = msEncrypt.ToArray();
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    private string DecryptString(string cipherText, string key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 确保密钥长度为32字节

        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    /// <summary>
    /// 生成随机密钥
    /// </summary>
    private string GenerateRandomKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// 令牌数据结构
    /// </summary>
    private class TokenData
    {
        public string TokenId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
