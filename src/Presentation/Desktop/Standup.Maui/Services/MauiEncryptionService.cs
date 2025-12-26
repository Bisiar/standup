using System.Security.Cryptography;
using System.Text;
using Serilog;
using Standup.Application.Interfaces;

namespace Standup.Maui.Services;

public sealed class MauiEncryptionService : IEncryptionService
{
    private const string EncryptionKeyStorageKey = "standup_encryption_key";
    private byte[]? _key;

    public string Encrypt(string plainText)
    {
        Log.Debug("MauiEncryptionService.Encrypt called");
        var key = GetOrCreateKey();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        Log.Debug("Encryption completed successfully");
        return Convert.ToBase64String(result);
    }

    public Task<string> EncryptAsync(string plainText) => Task.FromResult(Encrypt(plainText));

    public Task<string> DecryptAsync(string cipherText)
    {
        Log.Debug("MauiEncryptionService.DecryptAsync called");
        var key = GetOrCreateKey();
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        Log.Debug("Decryption completed successfully");
        return Task.FromResult(Encoding.UTF8.GetString(plainBytes));
    }

    // Use Preferences instead of SecureStorage for development (no provisioning profile needed)
    private static string? GetPreference(string key)
    {
        return Preferences.Default.Get<string?>(key, null);
    }

    private static void SetPreference(string key, string value)
    {
        Preferences.Default.Set(key, value);
    }

    private static byte[] GenerateKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return aes.Key;
    }

    private byte[] GetOrCreateKey()
    {
        if (_key != null)
        {
            Log.Debug("Returning cached encryption key");
            return _key;
        }

        Log.Debug("Attempting to read encryption key from Preferences");
        var storedKey = GetPreference(EncryptionKeyStorageKey);

        if (!string.IsNullOrEmpty(storedKey))
        {
            Log.Debug("Found existing encryption key in Preferences");
            _key = Convert.FromBase64String(storedKey);
            return _key;
        }

        Log.Information("Generating new encryption key");
        _key = GenerateKey();
        SetPreference(EncryptionKeyStorageKey, Convert.ToBase64String(_key));
        Log.Information("New encryption key saved to Preferences");

        return _key;
    }
}
