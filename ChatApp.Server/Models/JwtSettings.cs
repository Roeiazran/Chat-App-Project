namespace ChatApp.Server.Models;

public class JwtSettings
{
    public string SecretKey { get; set; } = null!;
    public int AccessTokenLifetimeMinutes { get; set; } = 2;
    public int RefreshTokenLifetimeMinutes { get; set; } = 15;
    public string Issuer { get; set; } = "ChatApp";
    public string Audience { get; set; } = "ChatAppUsers";
}