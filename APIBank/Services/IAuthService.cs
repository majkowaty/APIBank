using APIBank.Model;
using APIBank.Model.Auth;

public interface IAuthService
{
    AuthResponse Register(RegisterRequest request);
    AuthResponse Login(LoginRequest request);
    User? GetUserById(int userId);
    void LinkClient(int userId, string accountId);
}
