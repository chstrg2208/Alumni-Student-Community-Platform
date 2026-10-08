using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
// using Microsoft.OpenApi.Models;
using Microsoft.OpenApi;
using UserService.Data;
using UserService.Middleware;
using UserService.Models;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context (PostgreSQL)
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Application Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMemberService, MemberService>();

// 3. JWT Authentication
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
        ValidateIssuer = false, // Set to true with valid issuer in prod
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 4. CORS for Frontend (Khải) & Mobile (Tín)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Swagger with Bearer Token support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AlumniConnect - User Service API",
        Version = "v1",
        Description = "Microservice quản lý Tài khoản, Đăng nhập, Hồ sơ cá nhân (US-01 đến US-10) - Đảm nhận bởi Chí Trung"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Token theo định dạng: Bearer {token}",
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

// Ensure Database Created & Seed Sample Data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            var sampleAlumni = new User
            {
                Email = "alumni.nam@fpt.edu.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                FullName = "Nguyễn Hoàng Nam",
                Campus = "Hòa Lạc",
                Major = "Kỹ thuật phần mềm",
                Batch = "K14",
                Bio = "Senior Software Engineer tại FPT Software. Sẵn sàng chia sẻ kinh nghiệm phỏng vấn & CV.",
                Role = "Alumni",
                IsEmailVerified = true,
                PrivacySetting = new UserPrivacySetting { ShowEmail = true, ShowBio = true, ShowCampus = true, ShowMajor = true, ShowBatch = true }
            };

            var sampleStudent = new User
            {
                Email = "student.trung@fpt.edu.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                FullName = "Chí Trung",
                Campus = "Hồ Chí Minh",
                Major = "Kỹ thuật phần mềm",
                Batch = "K17",
                Bio = "Sinh viên năm 3 quan tâm tới kiến trúc Microservices và Cloud.",
                Role = "Student",
                IsEmailVerified = true,
                PrivacySetting = new UserPrivacySetting { ShowEmail = false, ShowBio = true, ShowCampus = true, ShowMajor = true, ShowBatch = true }
            };

            db.Users.AddRange(sampleAlumni, sampleStudent);
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Chưa kết nối được tới PostgreSQL (Users DB): {Message}. Hãy chắc chắn PostgreSQL đang chạy.", ex.Message);
    }
}

// HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AlumniConnect User Service v1");
    c.RoutePrefix = string.Empty; // Swagger as home page
});

app.UseCors("AllowAll");

app.UseMiddleware<TokenRevocationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
