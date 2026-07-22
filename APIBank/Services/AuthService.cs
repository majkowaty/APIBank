using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using APIBank.Model;
using APIBank.Model.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public AuthResponse Register(RegisterRequest request)
    {
        if (_context.Users.Any(u => u.Username == request.Username))
            throw new Exception("Username already taken");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Username = user.Username,
            ClientAccountId = user.ClientAccountId
        };
    }

    public AuthResponse Login(LoginRequest request)
    {
        var user = _context.Users
            .FirstOrDefault(u => u.Username == request.Username)
            ?? throw new Exception("Invalid username or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid username or password");

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Username = user.Username,
            ClientAccountId = user.ClientAccountId
        };
    }

    public User? GetUserById(int userId) =>
        _context.Users
            .Include(u => u.Client)
            .FirstOrDefault(u => u.Id == userId);

    public void LinkClient(int userId, string accountId)
    {
        var user = _context.Users.Find(userId)
            ?? throw new Exception("User not found");

        if (_context.Users.Any(u => u.ClientAccountId == accountId && u.Id != userId))
            throw new Exception("This bank account is already linked to another user");

        var client = _context.Clients.Find(accountId)
            ?? throw new Exception($"Client {accountId} not found");

        user.ClientAccountId = accountId;
        client.UserId = userId;
        _context.SaveChanges();
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                double.Parse(_config["Jwt:ExpiryHours"] ?? "24")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
