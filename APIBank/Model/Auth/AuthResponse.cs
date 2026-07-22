namespace APIBank.Model.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ClientAccountId { get; set; }
    }
}
