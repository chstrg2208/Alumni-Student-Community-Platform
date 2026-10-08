using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IProfileService
{
    Task<MyProfileResponse> GetMyProfileAsync(Guid userId);
    Task<MyProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<PublicProfileResponse> GetPublicProfileAsync(Guid targetUserId, Guid? currentUserId = null);
    Task<PrivacySettingDto> GetPrivacySettingsAsync(Guid userId);
    Task<PrivacySettingDto> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingRequest request);
}

public class ProfileService : IProfileService
{
    private readonly UserDbContext _dbContext;

    public ProfileService(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MyProfileResponse> GetMyProfileAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .Include(u => u.PrivacySetting)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var setting = user.PrivacySetting ?? new UserPrivacySetting();

        return new MyProfileResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Campus,
            user.Major,
            user.Batch,
            user.Bio,
            user.Role,
            user.IsEmailVerified,
            user.CreatedAt,
            new PrivacySettingDto(
                setting.ShowEmail,
                setting.ShowCampus,
                setting.ShowMajor,
                setting.ShowBatch,
                setting.ShowBio
            )
        );
    }

    public async Task<MyProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _dbContext.Users
            .Include(u => u.PrivacySetting)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy người dùng.");

        user.FullName = request.FullName.Trim();
        user.AvatarUrl = request.AvatarUrl;
        user.Campus = request.Campus;
        user.Major = request.Major;
        user.Batch = request.Batch;
        user.Bio = request.Bio;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var setting = user.PrivacySetting ?? new UserPrivacySetting();

        return new MyProfileResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Campus,
            user.Major,
            user.Batch,
            user.Bio,
            user.Role,
            user.IsEmailVerified,
            user.CreatedAt,
            new PrivacySettingDto(
                setting.ShowEmail,
                setting.ShowCampus,
                setting.ShowMajor,
                setting.ShowBatch,
                setting.ShowBio
            )
        );
    }

    public async Task<PublicProfileResponse> GetPublicProfileAsync(Guid targetUserId, Guid? currentUserId = null)
    {
        // Check if target user has blocked current user (US-10)
        if (currentUserId.HasValue)
        {
            var isBlocked = await _dbContext.BlockedUsers
                .AnyAsync(b => b.BlockerId == targetUserId && b.BlockedUserId == currentUserId.Value);

            if (isBlocked)
            {
                throw new KeyNotFoundException("Hồ sơ không tồn tại hoặc bạn không có quyền xem hồ sơ này.");
            }
        }

        var user = await _dbContext.Users
            .Include(u => u.PrivacySetting)
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var setting = user.PrivacySetting ?? new UserPrivacySetting();

        return new PublicProfileResponse(
            user.Id,
            user.FullName,
            user.AvatarUrl,
            setting.ShowCampus ? user.Campus : null,
            setting.ShowMajor ? user.Major : null,
            setting.ShowBatch ? user.Batch : null,
            setting.ShowBio ? user.Bio : null,
            user.Role,
            setting.ShowEmail ? user.Email : null
        );
    }

    public async Task<PrivacySettingDto> GetPrivacySettingsAsync(Guid userId)
    {
        var setting = await _dbContext.PrivacySettings.FirstOrDefaultAsync(p => p.UserId == userId);
        if (setting == null)
        {
            setting = new UserPrivacySetting { UserId = userId };
            _dbContext.PrivacySettings.Add(setting);
            await _dbContext.SaveChangesAsync();
        }

        return new PrivacySettingDto(
            setting.ShowEmail,
            setting.ShowCampus,
            setting.ShowMajor,
            setting.ShowBatch,
            setting.ShowBio
        );
    }

    public async Task<PrivacySettingDto> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingRequest request)
    {
        var setting = await _dbContext.PrivacySettings.FirstOrDefaultAsync(p => p.UserId == userId);
        if (setting == null)
        {
            setting = new UserPrivacySetting { UserId = userId };
            _dbContext.PrivacySettings.Add(setting);
        }

        setting.ShowEmail = request.ShowEmail;
        setting.ShowCampus = request.ShowCampus;
        setting.ShowMajor = request.ShowMajor;
        setting.ShowBatch = request.ShowBatch;
        setting.ShowBio = request.ShowBio;

        await _dbContext.SaveChangesAsync();

        return new PrivacySettingDto(
            setting.ShowEmail,
            setting.ShowCampus,
            setting.ShowMajor,
            setting.ShowBatch,
            setting.ShowBio
        );
    }
}
