namespace Standup.Domain.Entities;

public record EmailSettings(
    string FromAddress = "",
    string FromDisplayName = "",
    bool SendViaGraph = true);
