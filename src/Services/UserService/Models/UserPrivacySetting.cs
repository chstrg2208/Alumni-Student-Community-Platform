using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models;

public class UserPrivacySetting
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // Privacy controls corresponding to US-09
    public bool ShowEmail { get; set; } = false;
    public bool ShowCampus { get; set; } = true;
    public bool ShowMajor { get; set; } = true;
    public bool ShowBatch { get; set; } = true;
    public bool ShowBio { get; set; } = true;
}
