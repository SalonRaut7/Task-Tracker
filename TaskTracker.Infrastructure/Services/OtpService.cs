using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly OtpOptions _options;

    public OtpService(IOptions<OtpOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateOtp()
    {
        // Cryptographically secure 6-digit number
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1_000_000;
        return number.ToString("D6"); // zero-padded to 6 digits
    }

    public string HashOtp(string otp)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.HashKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(otp));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyOtp(string otp, string hash)
    {
        var computedHash = HashOtp(otp);
        return computedHash == hash;
    }
}
