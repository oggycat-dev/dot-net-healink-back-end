using System.Security.Cryptography;
using System.Text;

namespace SharedLibrary.Commons.Helpers;

/// <summary>
/// Helper for encrypting and decrypting passwords
/// Used for secure password transmission between services
/// </summary>
public static class PasswordCryptoHelper
{
    /// <summary>
    /// Encrypts plaintext password using AES encryption
    /// </summary>
    /// <param name="plainText">Password to encrypt</param>
    /// <param name="encryptKey">32-character encryption key</param>
    /// <returns>Base64 encoded encrypted password</returns>
    public static string Encrypt(string plainText, string encryptKey)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(encryptKey);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts encrypted password using AES decryption
    /// </summary>
    /// <param name="cipherText">Base64 encoded encrypted password</param>
    /// <param name="encryptKey">32-character encryption key</param>
    /// <returns>Decrypted plaintext password</returns>
    public static string Decrypt(string cipherText, string encryptKey)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));
        
        if (string.IsNullOrEmpty(encryptKey))
            throw new ArgumentException("Encryption key cannot be null or empty", nameof(encryptKey));

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(encryptKey);

            var ivLength = aes.BlockSize / 8;
            
            // Validate that we have enough data for IV + encrypted content
            if (fullCipher.Length < ivLength)
            {
                throw new ArgumentException($"Invalid cipher text: length {fullCipher.Length} is less than required IV length {ivLength}");
            }

            var iv = new byte[ivLength];
            var cipher = new byte[fullCipher.Length - ivLength];

            Array.Copy(fullCipher, iv, ivLength);
            Array.Copy(fullCipher, ivLength, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid Base64 cipher text format", nameof(cipherText), ex);
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("Failed to decrypt password - invalid key or corrupted data", nameof(cipherText), ex);
        }
    }
}
