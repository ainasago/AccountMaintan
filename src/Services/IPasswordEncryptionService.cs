using System.Security.Cryptography;

namespace WebUI.Services;

/// <summary>
/// 密码加密服务接口
/// </summary>
public interface IPasswordEncryptionService
{
    /// <summary>
    /// 生成加密令牌
    /// </summary>
    string GenerateEncryptionToken();

    /// <summary>
    /// 使用令牌加密密码
    /// </summary>
    string EncryptPassword(string password, string token);

    /// <summary>
    /// 使用令牌解密密码
    /// </summary>
    string DecryptPassword(string encryptedPassword, string token);

    /// <summary>
    /// 验证令牌是否有效
    /// </summary>
    bool IsTokenValid(string token);

    /// <summary>
    /// 获取令牌过期时间
    /// </summary>
    DateTime GetTokenExpiryTime(string token);
}
