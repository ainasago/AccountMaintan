using System.Security.Cryptography;
using System.Text;

namespace WebUI.Services;

/// <summary>
/// 加密服务
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// 加密字符串
    /// </summary>
    string Encrypt(string plainText);
    
    /// <summary>
    /// 解密字符串
    /// </summary>
    string Decrypt(string cipherText);
}

/// <summary>
/// 加密服务实现
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        // 从配置文件获取密钥，如果没有则生成默认密钥
        var keyString = configuration["Encryption:Key"] ?? "DefaultEncryptionKey123!@#";
        var ivString = configuration["Encryption:IV"] ?? "DefaultIV12345678";
        
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32, '0').Substring(0, 32));
        _iv = Encoding.UTF8.GetBytes(ivString.PadRight(16, '0').Substring(0, 16));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);

        swEncrypt.Write(plainText);
        swEncrypt.Flush();
        csEncrypt.FlushFinalBlock();

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch
        {
            // 如果解密失败，返回原文本（可能是未加密的）
            return cipherText;
        }
    }
}
