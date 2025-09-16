namespace ProductAuthMicroservice.Commons.Configs;

public class CorsConfig
{
    public string PolicyName { get; set; } = "DefaultCors";

    public bool AllowAnyOrigin { get; set; }
    public bool AllowAnyMethod { get; set; }
    public bool AllowAnyHeader { get; set; }

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();
    public string[] ExposedHeaders { get; set; } = Array.Empty<string>();

    public bool AllowCredentials { get; set; }
    public int? PreflightMaxAgeSeconds { get; set; } // Cache preflight
}
