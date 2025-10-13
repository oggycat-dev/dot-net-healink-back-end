namespace SubscriptionService.Application.Commons.Services;

/// <summary>
/// Service for generating QR codes from payment gateway data
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generate QR code from data string (e.g., MoMo qrCodeUrl)
    /// Returns Base64-encoded PNG image
    /// </summary>
    /// <param name="data">QR code data string</param>
    /// <param name="pixelsPerModule">Size of each module (default: 10)</param>
    /// <returns>Base64-encoded PNG image string</returns>
    string GenerateQrCodeBase64(string data, int pixelsPerModule = 10);
}

