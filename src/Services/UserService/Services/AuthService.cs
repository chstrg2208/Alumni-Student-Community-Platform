using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
    Task<bool> VerifyEmailAsync(VerifyEmailRequest request);
    Task<bool> ResendVerificationAsync(ResendVerificationRequest request);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task LogoutAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly UserDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserDbContext dbContext, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            throw new InvalidOperationException("Email này đã được sử dụng.");
        }

        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role,
            Campus = request.Campus,
            Major = request.Major,
            Batch = request.Batch,
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            PrivacySetting = new UserPrivacySetting()
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // In production, send email with verificationToken. Here we log it for demo & easy testing.
        _logger.LogInformation("Xác thực email cho {Email} với token: {Token}", user.Email, verificationToken);

        var (jwt, expiresAt) = _tokenService.GenerateJwtToken(user);
        return new AuthResponse(user.Id, user.Email, user.FullName, user.Role, jwt, expiresAt, user.IsEmailVerified);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
        }

        var (jwt, expiresAt) = _tokenService.GenerateJwtToken(user);
        return new AuthResponse(user.Id, user.Email, user.FullName, user.Role, jwt, expiresAt, user.IsEmailVerified);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            // Auto register user with Google profile
            user = new User
            {
                Email = normalizedEmail,
                FullName = request.FullName,
                AvatarUrl = request.AvatarUrl,
                GoogleId = request.IdToken.Length > 50 ? request.IdToken[..50] : request.IdToken,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role,
                IsEmailVerified = true, // Google already verified this email
                PrivacySetting = new UserPrivacySetting()
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(user.GoogleId))
        {
            user.GoogleId = request.IdToken.Length > 50 ? request.IdToken[..50] : request.IdToken;
            user.IsEmailVerified = true;
            await _dbContext.SaveChangesAsync();
        }

        var (jwt, expiresAt) = _tokenService.GenerateJwtToken(user);
        return new AuthResponse(user.Id, user.Email, user.FullName, user.Role, jwt, expiresAt, user.IsEmailVerified);
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (user.IsEmailVerified)
            return true;

        if (user.EmailVerificationToken != request.Token || user.VerificationTokenExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Mã xác minh không hợp lệ hoặc đã hết hạn.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.VerificationTokenExpiresAt = null;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ResendVerificationAsync(ResendVerificationRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (user.IsEmailVerified)
            return true;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationToken = token;
        user.VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Gửi lại mã xác minh cho {Email}: {Token}", user.Email, token);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (!string.IsNullOrEmpty(user.PasswordHash) && !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không chính xác.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(2);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Mã đặt lại mật khẩu cho {Email}: {Token}", user.Email, resetToken);
        return resetToken;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (user.PasswordResetToken != request.Token || user.ResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task LogoutAsync(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            await _tokenService.InvalidateTokenAsync(token);
        }
    }
}
