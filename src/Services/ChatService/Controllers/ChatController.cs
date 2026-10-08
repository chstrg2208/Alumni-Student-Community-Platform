using System.Security.Claims;
using ChatService.Data;
using ChatService.DTOs;
using ChatService.Hubs;
using ChatService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatDbContext _dbContext;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(ChatDbContext dbContext, IHubContext<ChatHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Lấy danh sách các phòng chat (gồm các phòng user tham gia và các phòng nhóm ngành công khai)
    /// </summary>
    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var rooms = await _dbContext.Rooms
            .Include(r => r.Members)
            .Include(r => r.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .Where(r => r.RoomType == "Group" || r.Members.Any(m => m.UserId == currentUserId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ChatRoomResponse(
                r.Id,
                r.Name,
                r.RoomType,
                r.Major,
                r.CreatedAt,
                r.Messages.Select(m => new ChatMessageResponse(
                    m.Id,
                    m.RoomId,
                    m.SenderId,
                    m.SenderName,
                    m.SenderAvatarUrl,
                    m.Content,
                    m.SentAt
                )).FirstOrDefault()
            ))
            .ToListAsync();

        return Ok(rooms);
    }

    /// <summary>
    /// Bắt đầu hoặc mở lại cuộc trò chuyện 1-1 giữa Sinh viên và Cựu sinh viên
    /// </summary>
    [HttpPost("rooms/direct")]
    public async Task<IActionResult> GetOrCreateDirectRoom([FromBody] CreateDirectChatRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";

        if (currentUserId == request.TargetUserId)
            return BadRequest(new { message = "Không thể tạo phòng chat 1-1 với chính mình." });

        // Tìm xem đã có phòng chat 1-1 giữa 2 người này chưa
        var existingRoom = await _dbContext.Rooms
            .Include(r => r.Members)
            .Where(r => r.RoomType == "Direct")
            .Where(r => r.Members.Any(m => m.UserId == currentUserId) && r.Members.Any(m => m.UserId == request.TargetUserId))
            .FirstOrDefaultAsync();

        if (existingRoom != null)
        {
            return Ok(new { roomId = existingRoom.Id, name = existingRoom.Name });
        }

        // Tạo phòng chat 1-1 mới
        var room = new ChatRoom
        {
            Name = $"{currentUserName} & {request.TargetUserName}",
            RoomType = "Direct"
        };

        room.Members.Add(new ChatMember { UserId = currentUserId, FullName = currentUserName });
        room.Members.Add(new ChatMember { UserId = request.TargetUserId, FullName = request.TargetUserName });

        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return Ok(new { roomId = room.Id, name = room.Name });
    }

    /// <summary>
    /// Tạo phòng chat nhóm (theo nhóm ngành hoặc thảo luận chung)
    /// </summary>
    [HttpPost("rooms/group")]
    public async Task<IActionResult> CreateGroupRoom([FromBody] CreateGroupChatRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";

        var room = new ChatRoom
        {
            Name = request.Name,
            RoomType = "Group",
            Major = request.Major
        };

        room.Members.Add(new ChatMember { UserId = currentUserId, FullName = currentUserName });

        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return Ok(new { roomId = room.Id, name = room.Name });
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn của một phòng chat (có phân trang)
    /// </summary>
    [HttpGet("rooms/{roomId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _dbContext.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync();
        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt) // Sắp xếp lại từ cũ đến mới để hiển thị tin nhắn
            .Select(m => new ChatMessageResponse(
                m.Id,
                m.RoomId,
                m.SenderId,
                m.SenderName,
                m.SenderAvatarUrl,
                m.Content,
                m.SentAt
            ))
            .ToListAsync();

        return Ok(new { items = messages, totalCount = total, page, pageSize });
    }

    /// <summary>
    /// Gửi tin nhắn qua REST API (và tự động phát sóng qua SignalR WebSocket tới mọi người trong phòng)
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid roomId, [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });

        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";

        var roomExists = await _dbContext.Rooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists)
            return NotFound(new { message = "Không tìm thấy phòng chat." });

        var msg = new ChatMessage
        {
            RoomId = roomId,
            SenderId = currentUserId,
            SenderName = currentUserName,
            Content = request.Content.Trim(),
            SentAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(msg);
        await _dbContext.SaveChangesAsync();

        var response = new ChatMessageResponse(
            msg.Id,
            msg.RoomId,
            msg.SenderId,
            msg.SenderName,
            msg.SenderAvatarUrl,
            msg.Content,
            msg.SentAt
        );

        // Phát sóng qua SignalR tới các client đang nghe phòng này
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", response);

        return Ok(response);
    }
}
