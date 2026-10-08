using System.Text;
using ChatService.Data;
using ChatService.Hubs;
using ChatService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context (SQLite)
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=chat.db"));

// 2. SignalR Service
builder.Services.AddSignalR();

// 3. JWT Authentication (supports WebSocket query parameter "access_token")
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AlumniConnect_SuperSecretKey_ForPRM_PRN_Project_2026_DotNetCore";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };

    // Cho phép SignalR đọc JWT token từ Query string "access_token" khi kết nối WebSocket
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 4. CORS: SignalR requires AllowCredentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Cho phép tất cả origin kết nối (FE PRN & Mobile PRM)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 5. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AlumniConnect - Chat Service API",
        Version = "v1",
        Description = "Microservice Chat Real-time & Lưu lịch sử tin nhắn (SignalR & REST) - Đảm nhận bởi Chí Trung"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Token: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Ensure DB Created & Seed Default Chat Rooms (Nhóm ngành & Phòng chung)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.EnsureCreated();

    if (!db.Rooms.Any())
    {
        db.Rooms.AddRange(
            new ChatRoom
            {
                Name = "Không gian chung - Toàn trường",
                RoomType = "Group",
                Major = "Chung"
            },
            new ChatRoom
            {
                Name = "Nhóm ngành Công nghệ thông tin (IT)",
                RoomType = "Group",
                Major = "Kỹ thuật phần mềm"
            },
            new ChatRoom
            {
                Name = "Nhóm ngành Kinh tế & Quản trị kinh doanh",
                RoomType = "Group",
                Major = "Quản trị kinh doanh"
            },
            new ChatRoom
            {
                Name = "Nhóm ngành Thiết kế đồ họa & Mỹ thuật số",
                RoomType = "Group",
                Major = "Thiết kế đồ họa"
            }
        );
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AlumniConnect Chat Service v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowSignalR");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
