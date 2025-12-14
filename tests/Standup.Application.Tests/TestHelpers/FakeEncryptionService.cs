using Standup.Application.Interfaces;

namespace Standup.Application.Tests.TestHelpers;

/// <summary>
/// Fake encryption service for testing - uses simple reversible encoding.
/// NO MOCKS - real test implementation.
/// </summary>
public sealed class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        return $"encrypted:{plainText}";
    }

    public Task<string> EncryptAsync(string plainText)
    {
        return Task.FromResult(Encrypt(plainText));
    }

    public Task<string> DecryptAsync(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return Task.FromResult(cipherText);
        }

        if (cipherText.StartsWith("encrypted:"))
        {
            return Task.FromResult(cipherText.Substring("encrypted:".Length));
        }

        return Task.FromResult(cipherText);
    }
}
