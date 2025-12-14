namespace Standup.Application.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string cipherText);
}
