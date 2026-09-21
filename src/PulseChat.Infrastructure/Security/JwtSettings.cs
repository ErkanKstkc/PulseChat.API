namespace PulseChat.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = "PulseChat_UltraSecureSecretKey_ThatIsAtLeast32BytesLong!";
    public string Issuer { get; set; } = "PulseChat.API";
    public string Audience { get; set; } = "PulseChat.Clients";
    public int ExpiryMinutes { get; set; } = 60;
}
