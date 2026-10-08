using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatService.Models;

public class ChatRoom
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// "Direct" (1-1) hoặc "Group" (Nhóm ngành hoặc Không gian chung)
    /// </summary>
    [Required]
    public string RoomType { get; set; } = "Group";

    /// <summary>
    /// Tên nhóm ngành (ví dụ "Kỹ thuật phần mềm", "Kinh tế", "Chung")
    /// </summary>
    public string? Major { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public ChatRoom? Room { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class ChatMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public ChatRoom? Room { get; set; }

    [Required]
    public Guid SenderId { get; set; }

    [Required]
    public string SenderName { get; set; } = string.Empty;

    public string? SenderAvatarUrl { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
