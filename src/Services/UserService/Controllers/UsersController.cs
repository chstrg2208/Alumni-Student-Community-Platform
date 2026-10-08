using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public UsersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    /// <summary>
    /// US-06: Tìm kiếm thành viên theo tên, campus, chuyên ngành để kết nối
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] UserSearchRequest request)
    {
        try
        {
            Guid? currentUserId = null;
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var parsedGuid))
            {
                currentUserId = parsedGuid;
            }

            var result = await _memberService.SearchMembersAsync(currentUserId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-10: Chặn tài khoản khác để hạn chế tương tác không mong muốn
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid id)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _memberService.BlockUserAsync(currentUserId, id);
            return Ok(new { message = "Đã chặn người dùng thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-10: Bỏ chặn người dùng
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}/block")]
    public async Task<IActionResult> UnblockUser(Guid id)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _memberService.UnblockUserAsync(currentUserId, id);
            if (success)
                return Ok(new { message = "Đã bỏ chặn người dùng thành công." });

            return NotFound(new { message = "Người dùng này chưa bị chặn." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-10: Lấy danh sách những người dùng mà bạn đã chặn
    /// </summary>
    [Authorize]
    [HttpGet("blocked")]
    public async Task<IActionResult> GetBlockedUsers()
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var list = await _memberService.GetBlockedUsersAsync(currentUserId);
            return Ok(list);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
