using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

public class BlockedUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BlockerId { get; set; }

    [ForeignKey(nameof(BlockerId))]
    public User? Blocker { get; set; }

    [Required]
    public Guid BlockedUserId { get; set; }

    [ForeignKey(nameof(BlockedUserId))]
    public User? Blocked { get; set; }

    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
}
