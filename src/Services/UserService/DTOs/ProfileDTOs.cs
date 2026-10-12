using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public record UpdateProfileRequest(
    [Required, MaxLength(100)] string FullName,
    string? AvatarUrl,
    string? Campus,
    string? Major,
    string? Batch,
    string? Bio,
    string? GoogleCalendarUrl // US-11
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
    string? GoogleCalendarUrl,
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
    string? GoogleCalendarUrl, // Shown if allowed by PrivacySetting
    string Role,
    string? Email = null // Only shown if allowed by PrivacySetting
);

public record PrivacySettingDto(
    bool ShowEmail,
    bool ShowCampus,
    bool ShowMajor,
    bool ShowBatch,
    bool ShowBio,
    bool ShowGoogleCalendar
);

public record UpdatePrivacySettingRequest(
    bool ShowEmail,
    bool ShowCampus,
    bool ShowMajor,
    bool ShowBatch,
    bool ShowBio,
    bool ShowGoogleCalendar
);
