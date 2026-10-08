using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Data;
using UserService.Models;

namespace UserService.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateJwtToken(User user);
    Task InvalidateTokenAsync(string token);
    Task<bool> IsTokenInvalidatedAsync(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserDbContext _dbContext;

    public TokenService(IConfiguration configuration, UserDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "AlumniConnect_SuperSecretKey_ForPRM_PRN_Project_2026";
        var issuer = _configuration["Jwt:Issuer"] ?? "AlumniConnect";
        var audience = _configuration["Jwt:Audience"] ?? "AlumniConnectClients";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(securityToken), expiresAt);
    }

    public async Task InvalidateTokenAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        DateTime expiryDate = DateTime.UtcNow.AddDays(7);
        try
        {
            var jwt = handler.ReadJwtToken(token);
            expiryDate = jwt.ValidTo;
        }
        catch
        {
            // fallback
        }

        _dbContext.InvalidatedTokens.Add(new InvalidatedToken
        {
            Token = token,
            InvalidatedAt = DateTime.UtcNow,
            ExpiryDate = expiryDate
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsTokenInvalidatedAsync(string token)
    {
        return await _dbContext.InvalidatedTokens.AnyAsync(t => t.Token == token);
    }
}
