namespace Standup.Infrastructure.Configuration;

public class EncryptionOptions
{
    public const string SectionName = "Encryption";

    public string EncryptionKey { get; set; } = string.Empty;
}
