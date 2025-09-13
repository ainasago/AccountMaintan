using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace WebUI.Services;

/// <summary>
/// TOTP服务接口
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// 生成TOTP密钥
    /// </summary>
    string GenerateSecretKey();
    
    /// <summary>
    /// 生成TOTP验证码
    /// </summary>
    string GenerateTotp(string secretKey);
    
    /// <summary>
    /// 验证TOTP验证码
    /// </summary>
    bool ValidateTotp(string secretKey, string totp, int window = 1);
    
    /// <summary>
    /// 生成二维码
    /// </summary>
    string GenerateQrCode(string accountName, string secretKey, string issuer = "AccountManager");
}

/// <summary>
/// TOTP服务实现
/// </summary>
public class TotpService : ITotpService
{
    public string GenerateSecretKey()
    {
        var random = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }

    public string GenerateTotp(string secretKey)
    {
        var counter = GetCounter();
        var hash = GenerateHash(secretKey, counter);
        var offset = hash[hash.Length - 1] & 0xf;
        var binary = ((hash[offset] & 0x7f) << 24) |
                     ((hash[offset + 1] & 0xff) << 16) |
                     ((hash[offset + 2] & 0xff) << 8) |
                     (hash[offset + 3] & 0xff);
        var totp = binary % 1000000;
        return totp.ToString("D6");
    }

    public bool ValidateTotp(string secretKey, string totp, int window = 1)
    {
        var currentTotp = GenerateTotp(secretKey);
        if (currentTotp == totp) return true;

        // 检查时间窗口内的其他验证码
        for (int i = 1; i <= window; i++)
        {
            var previousTotp = GenerateTotp(secretKey, -i);
            if (previousTotp == totp) return true;
        }

        return false;
    }

    public string GenerateQrCode(string accountName, string secretKey, string issuer = "AccountManager")
    {
        var otpauthUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";
        
        // 暂时返回一个简单的文本表示，避免QRCoder的依赖问题
        // TODO: 修复QRCoder引用问题
        return $"otpauth://totp/{issuer}:{accountName}?secret={secretKey}&issuer={issuer}";
    }

    private long GetCounter()
    {
        var epochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        var currentTicks = DateTime.UtcNow.Ticks;
        var timeStep = 30; // 30秒一个周期
        return (currentTicks - epochTicks) / (timeStep * 10000000);
    }

    private string GenerateTotp(string secretKey, int offset = 0)
    {
        var counter = GetCounter() + offset;
        var hash = GenerateHash(secretKey, counter);
        var offset2 = hash[hash.Length - 1] & 0xf;
        var binary = ((hash[offset2] & 0x7f) << 24) |
                     ((hash[offset2 + 1] & 0xff) << 16) |
                     ((hash[offset2 + 2] & 0xff) << 8) |
                     (hash[offset2 + 3] & 0xff);
        var totp = binary % 1000000;
        return totp.ToString("D6");
    }

    private byte[] GenerateHash(string secretKey, long counter)
    {
        var key = Convert.FromBase64String(secretKey);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(key);
        return hmac.ComputeHash(counterBytes);
    }
}
