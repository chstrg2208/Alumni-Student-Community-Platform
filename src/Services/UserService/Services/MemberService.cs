using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public interface IMemberService
{
    Task<PagedResult<UserSummaryResponse>> SearchMembersAsync(Guid? currentUserId, UserSearchRequest request);
    Task<bool> BlockUserAsync(Guid currentUserId, Guid targetUserId);
    Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId);
    Task<List<BlockedUserDto>> GetBlockedUsersAsync(Guid currentUserId);
}

public class MemberService : IMemberService
{
    private readonly UserDbContext _dbContext;

    public MemberService(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<UserSummaryResponse>> SearchMembersAsync(Guid? currentUserId, UserSearchRequest request)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        // If user is logged in, exclude users who blocked current user or whom current user blocked
        if (currentUserId.HasValue)
        {
            var blockedIds = await _dbContext.BlockedUsers
                .Where(b => b.BlockerId == currentUserId.Value || b.BlockedUserId == currentUserId.Value)
                .Select(b => b.BlockerId == currentUserId.Value ? b.BlockedUserId : b.BlockerId)
                .ToListAsync();

            query = query.Where(u => !blockedIds.Contains(u.Id));
        }

        // Filter by keyword (Name or Email)
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(kw) || u.Email.ToLower().Contains(kw));
        }

        // Filter by Campus
        if (!string.IsNullOrWhiteSpace(request.Campus))
        {
            var campus = request.Campus.Trim().ToLower();
            query = query.Where(u => u.Campus != null && u.Campus.ToLower().Contains(campus));
        }

        // Filter by Major
        if (!string.IsNullOrWhiteSpace(request.Major))
        {
            var major = request.Major.Trim().ToLower();
            query = query.Where(u => u.Major != null && u.Major.ToLower().Contains(major));
        }

        // Filter by Role
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(u => u.Role == request.Role);
        }

        var totalCount = await query.CountAsync();
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize is > 0 and <= 50 ? request.PageSize : 10;

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.FullName,
                u.AvatarUrl,
                u.Campus,
                u.Major,
                u.Batch,
                u.Role
            ))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<UserSummaryResponse>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<bool> BlockUserAsync(Guid currentUserId, Guid targetUserId)
    {
        if (currentUserId == targetUserId)
            throw new InvalidOperationException("Bạn không thể tự chặn chính mình.");

        var targetExists = await _dbContext.Users.AnyAsync(u => u.Id == targetUserId);
        if (!targetExists)
            throw new KeyNotFoundException("Không tìm thấy người dùng cần chặn.");

        var alreadyBlocked = await _dbContext.BlockedUsers
            .AnyAsync(b => b.BlockerId == currentUserId && b.BlockedUserId == targetUserId);

        if (alreadyBlocked)
            return true;

        _dbContext.BlockedUsers.Add(new BlockedUser
        {
            BlockerId = currentUserId,
            BlockedUserId = targetUserId,
            BlockedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnblockUserAsync(Guid currentUserId, Guid targetUserId)
    {
        var blockRecord = await _dbContext.BlockedUsers
            .FirstOrDefaultAsync(b => b.BlockerId == currentUserId && b.BlockedUserId == targetUserId);

        if (blockRecord == null)
            return false;

        _dbContext.BlockedUsers.Remove(blockRecord);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<BlockedUserDto>> GetBlockedUsersAsync(Guid currentUserId)
    {
        return await _dbContext.BlockedUsers
            .Where(b => b.BlockerId == currentUserId)
            .Include(b => b.Blocked)
            .Select(b => new BlockedUserDto(
                b.BlockedUserId,
                b.Blocked != null ? b.Blocked.FullName : "Người dùng ẩn",
                b.Blocked != null ? b.Blocked.AvatarUrl : null,
                b.BlockedAt
            ))
            .ToListAsync();
    }
}
