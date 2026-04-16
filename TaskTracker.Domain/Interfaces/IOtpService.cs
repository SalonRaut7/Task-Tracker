namespace TaskTracker.Domain.Interfaces;

/// Generates and verifies OTP codes with secure hashing.
public interface IOtpService
{
    ///Generates a random 6-digit OTP code.
    string GenerateOtp();

    ///Hashes an OTP code for secure storage.
    string HashOtp(string otp);

    ///Verifies a plain OTP against its hash.
    bool VerifyOtp(string otp, string hash);
}
