using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public record UpdateProfileRequest(
    [Required, MaxLength(100)] string FullName,
    string? AvatarUrl,
    string? Campus,
    string? Major,
    string? Batch,
    string? Bio
);

public record MyProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    string? Campus,
    string? Major,
    string? Batch,
    string? Bio,
    string Role,
    bool IsEmailVerified,
    DateTime CreatedAt,
    PrivacySettingDto PrivacySetting
);

public record PublicProfileResponse(
    Guid Id,
    string FullName,
    string? AvatarUrl,
    string? Campus,
    string? Major,
    string? Batch,
    string? Bio,
    string Role,
    string? Email = null // Only shown if allowed by PrivacySetting
);

public record PrivacySettingDto(
    bool ShowEmail,
    bool ShowCampus,
    bool ShowMajor,
    bool ShowBatch,
    bool ShowBio
);

public record UpdatePrivacySettingRequest(
    bool ShowEmail,
    bool ShowCampus,
    bool ShowMajor,
    bool ShowBatch,
    bool ShowBio
);
