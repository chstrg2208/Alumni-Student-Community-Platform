using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Lấy thông tin hồ sơ của chính mình
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _profileService.GetMyProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-04: Cập nhật hồ sơ cá nhân gồm tên, avatar, campus, chuyên ngành, khóa và giới thiệu bản thân
    /// </summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var updatedProfile = await _profileService.UpdateMyProfileAsync(userId, request);
            return Ok(new { message = "Cập nhật hồ sơ thành công.", data = updatedProfile });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-05: Xem hồ sơ của sinh viên/cựu sinh viên khác (Tự động lọc theo quyền riêng tư và chặn tương tác)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublicProfile(Guid id)
    {
        try
        {
            Guid? currentUserId = null;
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var parsedGuid))
            {
                currentUserId = parsedGuid;
            }

            var profile = await _profileService.GetPublicProfileAsync(id, currentUserId);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-09: Xem cài đặt quyền riêng tư của hồ sơ
    /// </summary>
    [Authorize]
    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacySettings()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var settings = await _profileService.GetPrivacySettingsAsync(userId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// US-09: Tùy chỉnh thông tin nào được hiển thị công khai trên profile
    /// </summary>
    [Authorize]
    [HttpPut("privacy")]
    public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var settings = await _profileService.UpdatePrivacySettingsAsync(userId, request);
            return Ok(new { message = "Cập nhật quyền riêng tư thành công.", data = settings });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
