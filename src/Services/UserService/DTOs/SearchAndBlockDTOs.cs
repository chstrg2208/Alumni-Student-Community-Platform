namespace UserService.DTOs;

public record UserSearchRequest(
    string? Keyword = null,
    string? Campus = null,
    string? Major = null,
    string? Role = null,
    int Page = 1,
    int PageSize = 10
);

public record UserSummaryResponse(
    Guid Id,
    string FullName,
    string? AvatarUrl,
    string? Campus,
    string? Major,
    string? Batch,
    string Role
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record BlockedUserDto(
    Guid BlockedUserId,
    string FullName,
    string? AvatarUrl,
    DateTime BlockedAt
);
