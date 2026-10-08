# Alumni Student Community Platform

Hệ thống mạng xã hội kết nối Cựu sinh viên (Alumni) và Sinh viên (Student).

## Phân công trách nhiệm
- **Nguyễn Chí Trung:**
  - **User Service:** Quản lý tài khoản, Đăng nhập (Email, Google), Xác minh email, Hồ sơ cá nhân, Tìm kiếm thành viên, Quyền riêng tư, Chặn tài khoản (US-01 đến US-10).
  - **Chat Service:** Real-time chat (SignalR WebSocket), Chat 1-1 giữa Sinh viên & Cựu sinh viên, Chat nhóm ngành & không gian chung, Lưu lịch sử tin nhắn.
  - **Architecture:** Thiết kế kiến trúc tổng thể & chi tiết chuẩn C4 Model (`ARCHITECTURE_C4.md`).
- **Mai Tuấn Vinh:** Forum Service + AI Service
- **Admin & Team Lead:** Admin Service + User Stories
- **Nguyễn Phương Khải:** Front-end Web (PRN)
- **Đỗ Trọng Tín:** Mobile App (PRM)

---

## Kiến trúc hệ thống
Chi tiết xem tại tài liệu [ARCHITECTURE_C4.md](./ARCHITECTURE_C4.md) (Context, Container, Component C4 Diagrams).

---

## Hướng dẫn cài đặt & Chạy Local

### Yêu cầu
- .NET SDK 8.0 / 9.0 / 10.0

### Khởi chạy dịch vụ

1. **Khởi chạy User Service:**
```bash
dotnet run --project src/Services/UserService/UserService.csproj --urls "http://localhost:5164"
```
Swagger UI: [http://localhost:5164](http://localhost:5164)

2. **Khởi chạy Chat Service:**
```bash
dotnet run --project src/Services/ChatService/ChatService.csproj --urls "http://localhost:5199"
```
Swagger UI: [http://localhost:5199](http://localhost:5199)
SignalR Hub: `ws://localhost:5199/hubs/chat`
