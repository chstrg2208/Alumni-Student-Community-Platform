using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? Campus { get; set; } // e.g. "Hòa Lạc", "Hồ Chí Minh", "Đà Nẵng", "Quy Nhơn", "Cần Thơ"

    public string? Major { get; set; } // e.g. "Kỹ thuật phần mềm", "Trí tuệ nhân tạo", "Quản trị kinh doanh"

    public string? Batch { get; set; } // e.g. "K16", "K17" (Khóa học)

    public string? Bio { get; set; }

    public string Role { get; set; } = "Student"; // "Student", "Alumni", "Admin"

    public bool IsEmailVerified { get; set; } = false;

    public string? EmailVerificationToken { get; set; }

    public DateTime? VerificationTokenExpiresAt { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? ResetTokenExpiresAt { get; set; }

    public string? GoogleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public UserPrivacySetting? PrivacySetting { get; set; }

    public ICollection<BlockedUser> BlockedUsers { get; set; } = new List<BlockedUser>();
}
