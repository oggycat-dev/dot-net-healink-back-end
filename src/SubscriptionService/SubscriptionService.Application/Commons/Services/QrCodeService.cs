using QRCoder;

namespace SubscriptionService.Application.Commons.Services;

/// <summary>
/// Service for generating QR codes using QRCoder library
/// Generates QR codes from payment gateway data (e.g., MoMo qrCodeUrl)
/// </summary>
public class QrCodeService : IQrCodeService
{
    /// <summary>
    /// Generate QR code from data string
    /// </summary>
    /// <param name="data">QR code data (e.g., MoMo payment URL)</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 10 = medium size)</param>
    /// <returns>Base64-encoded PNG image that can be used directly in img src</returns>
    public string GenerateQrCodeBase64(string data, int pixelsPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException("QR code data cannot be empty", nameof(data));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        // Generate PNG bytes
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        
        // Convert to Base64 for direct use in HTML img tag
        return Convert.ToBase64String(qrCodeBytes);
    }
}

