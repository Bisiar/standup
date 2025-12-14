using Standup.Domain.Enums;

namespace Standup.Domain.Interfaces;

public record SummaryOptions(
    SummaryType Type = SummaryType.Technical,
    string? CustomPrompt = null,
    bool IncludeBlockers = true,
    bool IncludeNextSteps = true,
    SummaryTone Tone = SummaryTone.Professional,
    int MaxLength = 500);
