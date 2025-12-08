namespace Standup.Domain.Interfaces;

public record SummaryOptions(
    string? CustomPrompt = null,
    bool IncludeBlockers = true,
    bool IncludeNextSteps = true,
    SummaryTone Tone = SummaryTone.Professional,
    int MaxLength = 500);
