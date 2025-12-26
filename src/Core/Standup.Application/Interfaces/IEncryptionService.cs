namespace Standup.Application.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plain text synchronously.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>The encrypted cipher text.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Encrypts plain text asynchronously.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>The encrypted cipher text.</returns>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    /// Decrypts cipher text asynchronously.
    /// </summary>
    /// <param name="cipherText">The cipher text to decrypt.</param>
    /// <returns>The decrypted plain text.</returns>
    Task<string> DecryptAsync(string cipherText);
}
