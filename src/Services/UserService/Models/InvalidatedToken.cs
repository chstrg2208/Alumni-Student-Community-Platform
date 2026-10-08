using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class InvalidatedToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime InvalidatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiryDate { get; set; }
}
