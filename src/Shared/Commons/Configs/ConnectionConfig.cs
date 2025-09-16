namespace ProductAuthMicroservice.Commons.Configs;

public class ConnectionConfig
{
    public string DefaultConnection { get; set; } = string.Empty;
    public bool RetryOnFailure { get; set; } 
    public int MaxRetryCount { get; set; } 
    public int MaxRetryDelay { get; set; }
    public ICollection<string> ErrorNumbersToAdd { get; set; } = Array.Empty<string>();
}
