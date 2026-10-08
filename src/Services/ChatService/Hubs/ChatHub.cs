using System.Security.Claims;
using ChatService.Data;
using ChatService.DTOs;
using ChatService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ChatDbContext dbContext, ILogger<ChatHub> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";

        _logger.LogInformation("Người dùng kết nối SignalR: {UserId} ({UserName})", userId, userName);

        // Tự động thêm connection vào Group riêng của user để nhận tin nhắn trực tiếp
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Tham gia vào một phòng chat (Group ngành, không gian chung, hoặc phòng 1-1)
    /// </summary>
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";
        _logger.LogInformation("{UserName} đã vào phòng chat {RoomId}", userName, roomId);

        await Clients.Group(roomId).SendAsync("UserJoined", new
        {
            RoomId = roomId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Rời khỏi một phòng chat
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";

        await Clients.Group(roomId).SendAsync("UserLeft", new
        {
            RoomId = roomId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gửi tin nhắn vào một phòng chat (Room)
    /// </summary>
    public async Task SendMessage(string roomId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || !Guid.TryParse(roomId, out var parsedRoomId))
            return;

        var userIdString = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var senderId))
            return;

        var senderName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Ẩn danh";

        var message = new ChatMessage
        {
            RoomId = parsedRoomId,
            SenderId = senderId,
            SenderName = senderName,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        var response = new ChatMessageResponse(
            message.Id,
            message.RoomId,
            message.SenderId,
            message.SenderName,
            message.SenderAvatarUrl,
            message.Content,
            message.SentAt
        );

        // Broadcast tin nhắn tới tất cả thành viên đang ở trong phòng này
        await Clients.Group(roomId).SendAsync("ReceiveMessage", response);
    }

    /// <summary>
    /// Báo trạng thái đang nhập tin nhắn (Typing indicator)
    /// </summary>
    public async Task SendTyping(string roomId, bool isTyping)
    {
        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";
        await Clients.OthersInGroup(roomId).SendAsync("UserTyping", new
        {
            RoomId = roomId,
            UserName = userName,
            IsTyping = isTyping
        });
    }
}
