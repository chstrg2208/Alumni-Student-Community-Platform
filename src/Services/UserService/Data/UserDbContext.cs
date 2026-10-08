using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPrivacySetting> PrivacySettings => Set<UserPrivacySetting>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<InvalidatedToken> InvalidatedTokens => Set<InvalidatedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // 1-1 relationship with PrivacySetting
        modelBuilder.Entity<User>()
            .HasOne(u => u.PrivacySetting)
            .WithOne(p => p.User)
            .HasForeignKey<UserPrivacySetting>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Blocked relationship configuration
        modelBuilder.Entity<BlockedUser>()
            .HasOne(b => b.Blocker)
            .WithMany(u => u.BlockedUsers)
            .HasForeignKey(b => b.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BlockedUser>()
            .HasOne(b => b.Blocked)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BlockedUser>()
            .HasIndex(b => new { b.BlockerId, b.BlockedUserId })
            .IsUnique();
    }
}
