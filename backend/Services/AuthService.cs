using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExamBuilderAI.API.Data;
using ExamBuilderAI.API.DTOs.Auth;
using ExamBuilderAI.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ExamBuilderAI.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Check if username already exists
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return null;

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    public async Task<UserInfo?> GetUserByIdAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName
        };
    }

    private AuthResponse GenerateAuthResponse(User user)
    {
        var token = GenerateJwtToken(user);
        return new AuthResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "ExamBuilderAI_SuperSecret_Key_2024_MustBe32Chars!!")
        );

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "ExamBuilderAI",
            audience: _config["Jwt:Audience"] ?? "ExamBuilderAI",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
