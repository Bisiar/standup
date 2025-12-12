using System.Security.Cryptography;
using System.Text;
using Standup.Application.Interfaces;

namespace Standup.Maui.Services;

public sealed class MauiEncryptionService : IEncryptionService
{
    private const string EncryptionKeyStorageKey = "standup_encryption_key";
    private byte[]? _key;

    public string Encrypt(string plainText)
    {
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

        return Convert.ToBase64String(result);
    }

    public Task<string> EncryptAsync(string plainText) => Task.FromResult(Encrypt(plainText));

    public Task<string> DecryptAsync(string cipherText)
    {
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

        return Task.FromResult(Encoding.UTF8.GetString(plainBytes));
    }

    private byte[] GetOrCreateKey()
    {
        if (_key != null)
            return _key;

        var storedKey = SecureStorage.Default.GetAsync(EncryptionKeyStorageKey).GetAwaiter().GetResult();

        if (!string.IsNullOrEmpty(storedKey))
        {
            _key = Convert.FromBase64String(storedKey);
            return _key;
        }

        _key = GenerateKey();
        SecureStorage.Default.SetAsync(EncryptionKeyStorageKey, Convert.ToBase64String(_key)).GetAwaiter().GetResult();

        return _key;
    }

    private static byte[] GenerateKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return aes.Key;
    }
}
