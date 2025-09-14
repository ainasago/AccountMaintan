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
        
        // 从配置中获取加密密钥，如果没有则使用默认密钥（与前端保持一致）
        _encryptionKey = _configuration["PasswordEncryption:Key"] ?? "AccountManagerPasswordEncryptionKey2024!";
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
            _logger.LogDebug("开始解密密码，加密密码长度: {Length}, 令牌长度: {TokenLength}", 
                encryptedPassword?.Length ?? 0, token?.Length ?? 0);

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
                _logger.LogWarning("令牌验证失败，令牌: {Token}", token);
                throw new InvalidOperationException("令牌已过期或无效");
            }

            _logger.LogDebug("令牌验证通过，开始解密数据");

            // 解密密码（使用AES-CBC方式，与前端保持一致）
            var decryptedData = DecryptAesCbc(encryptedPassword, _encryptionKey);
            _logger.LogDebug("数据解密完成，解密后长度: {Length}", decryptedData?.Length ?? 0);
            
            var parts = decryptedData.Split(':', 2);
            _logger.LogDebug("分割后部分数量: {Count}", parts.Length);
            
            if (parts.Length != 2)
            {
                _logger.LogError("解密数据格式无效，分割后部分数量: {Count}, 数据: {Data}", parts.Length, decryptedData);
                throw new InvalidOperationException("解密数据格式无效");
            }

            var password = parts[0];
            var tokenFromData = parts[1];

            _logger.LogDebug("提取的密码长度: {PasswordLength}, 令牌长度: {TokenLength}", 
                password?.Length ?? 0, tokenFromData?.Length ?? 0);

            // 验证令牌是否匹配
            if (token != tokenFromData)
            {
                _logger.LogError("令牌不匹配，期望: {Expected}, 实际: {Actual}", token, tokenFromData);
                throw new InvalidOperationException("令牌不匹配");
            }

            _logger.LogDebug("密码解密成功");
            return password;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码解密失败，加密密码: {EncryptedPassword}, 令牌: {Token}", 
                encryptedPassword, token);
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
        
        // 使用SHA-256哈希生成密钥（与前端保持一致）
        using var sha256 = SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        aes.Key = keyBytes;
        
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
        
        // 使用SHA-256哈希生成密钥（与前端保持一致）
        using var sha256 = SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        aes.Key = keyBytes;

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
    /// AES-CBC解密（与前端Web Crypto API兼容）
    /// </summary>
    private string DecryptAesCbc(string cipherText, string key)
    {
        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            
            // 提取IV（前16字节）和密文
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];
            
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

            // 使用AES-CBC解密（与前端保持一致的密钥生成方式）
            using var aes = Aes.Create();
            
            // 使用SHA-256哈希生成密钥（与前端保持一致）
            using var sha256 = SHA256.Create();
            var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            aes.Key = keyBytes;
            
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES-CBC解密失败");
            throw;
        }
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
