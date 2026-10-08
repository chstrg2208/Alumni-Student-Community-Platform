using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string FullName,
    string? Role = "Student", // "Student" or "Alumni"
    string? Campus = null,
    string? Major = null,
    string? Batch = null
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record GoogleLoginRequest(
    [Required] string IdToken,
    [Required] string Email,
    [Required] string FullName,
    string? AvatarUrl = null,
    string? Role = "Student"
);

public record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Token,
    DateTime ExpiresAt,
    bool IsEmailVerified
);

public record VerifyEmailRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token
);

public record ResendVerificationRequest(
    [Required, EmailAddress] string Email
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword
);

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword
);
