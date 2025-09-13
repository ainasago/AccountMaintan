using System.Security.Cryptography;
using System.Text;

namespace WebUI.Services;

/// <summary>
/// 安全加密服务接口
/// </summary>
public interface ISecureEncryptionService
{
    /// <summary>
    /// 加密字符串
    /// </summary>
    string Encrypt(string plainText);
    
    /// <summary>
    /// 解密字符串
    /// </summary>
    string Decrypt(string cipherText);
    
    /// <summary>
    /// 生成新的加密密钥
    /// </summary>
    (string Key, string IV) GenerateNewKeys();
}

/// <summary>
/// 安全加密服务实现
/// </summary>
public class SecureEncryptionService : ISecureEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<SecureEncryptionService> _logger;

    public SecureEncryptionService(IConfiguration configuration, ILogger<SecureEncryptionService> logger)
    {
        _logger = logger;
        
        // 从环境变量或配置文件获取密钥
        var keyString = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") 
            ?? configuration["Encryption:Key"] 
            ?? throw new InvalidOperationException("加密密钥未配置，请设置ENCRYPTION_KEY环境变量");
        
        // 确保密钥长度为32字节（AES-256）
        _key = DeriveKey(keyString, 32);
        
        _logger.LogDebug("安全加密服务已初始化");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // 每次加密都生成新的IV
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // 将IV写入流的前16字节
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8);

            swEncrypt.Write(plainText);
            swEncrypt.Flush();
            csEncrypt.FlushFinalBlock();

            var encrypted = msEncrypt.ToArray();
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加密失败");
            throw new CryptographicException("加密过程中发生错误", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);
            
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 从加密数据的前16字节提取IV
            var iv = new byte[16];
            Array.Copy(encryptedBytes, 0, iv, 0, 16);
            aes.IV = iv;

            // 提取实际的加密数据
            var encryptedData = new byte[encryptedBytes.Length - 16];
            Array.Copy(encryptedBytes, 16, encryptedData, 0, encryptedData.Length);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密失败");
            // 如果解密失败，返回原文本（可能是未加密的旧数据）
            return cipherText;
        }
    }

    public (string Key, string IV) GenerateNewKeys()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        
        var key = Convert.ToBase64String(aes.Key);
        var iv = Convert.ToBase64String(aes.IV);
        
        _logger.LogInformation("生成了新的加密密钥对");
        return (key, iv);
    }

    /// <summary>
    /// 从密码派生密钥
    /// </summary>
    private byte[] DeriveKey(string password, int keyLength)
    {
        // 使用PBKDF2派生密钥
        using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("AccountManagerSalt"), 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyLength);
    }
}
