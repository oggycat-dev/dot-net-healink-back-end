namespace ProductAuthMicroservice.Commons.Configs;

public class JwtConfig
{
    public const string SectionName = "JwtConfig";
    
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpireInMinutes { get; set; } = 60;
    public int RefreshTokenExpireInDays { get; set; } = 7;
    
    // Validation settings
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public int ClockSkewMinutes { get; set; } = 5;
}