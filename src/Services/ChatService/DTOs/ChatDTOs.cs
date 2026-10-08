using System.ComponentModel.DataAnnotations;

namespace ChatService.DTOs;

public record CreateDirectChatRequest(
    [Required] Guid TargetUserId,
    [Required] string TargetUserName
);

public record CreateGroupChatRequest(
    [Required] string Name,
    string? Major = null
);

public record SendMessageRequest(
    [Required] string Content
);

public record ChatRoomResponse(
    Guid Id,
    string Name,
    string RoomType,
    string? Major,
    DateTime CreatedAt,
    ChatMessageResponse? LastMessage
);

public record ChatMessageResponse(
    Guid Id,
    Guid RoomId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarUrl,
    string Content,
    DateTime SentAt
);
