using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Configuration;

namespace Standup.Infrastructure.AI;

public class AIFoundrySummaryService : IAISummaryService
{
    private readonly AIFoundryOptions _options;

    public AIFoundrySummaryService(IOptions<AIFoundryOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> GenerateSummaryAsync(
        StandupData data,
        SummaryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new SummaryOptions();

        var client = CreateClient();
        var chatClient = client.GetChatClient(_options.DeploymentName);

        var systemPrompt = BuildSystemPrompt(options);
        var userPrompt = BuildUserPrompt(data);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await chatClient.CompleteChatAsync(
            messages,
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = options.MaxLength * 2,
                Temperature = 0.7f
            },
            cancellationToken);

        return completion.Value.Content[0].Text;
    }

    private static string BuildSystemPrompt(SummaryOptions options)
    {
        var toneDescription = options.Tone switch
        {
            SummaryTone.Casual => "casual and friendly, as if talking to coworkers",
            SummaryTone.Brief => "extremely concise, using bullet points only",
            _ => "professional but natural"
        };

        var basePrompt = options.Type switch
        {
            SummaryType.Executive => BuildExecutivePrompt(toneDescription, options),
            SummaryType.CodeReview => BuildCodeReviewPrompt(toneDescription, options),
            _ => BuildTechnicalPrompt(toneDescription, options) // Technical is default
        };

        if (!string.IsNullOrEmpty(options.CustomPrompt))
        {
            basePrompt += $"\n\nAdditional instructions: {options.CustomPrompt}";
        }

        return basePrompt;
    }

    private static string BuildTechnicalPrompt(string toneDescription, SummaryOptions options)
    {
        return $@"You are a helpful assistant that summarizes developer work activity for a technical audience.

Generate a {toneDescription} developer-focused standup update based on the work data provided.

Focus on TECHNICAL DETAILS that matter to developers:
- Specific code changes, APIs modified, or architecture decisions
- Technical debt addressed or introduced
- Performance improvements or regressions
- Dependencies updated or added
- Breaking changes or migration notes

Structure the update as:
1. **Code Changes** - Summarize commits with technical details: what was changed, which modules/components, APIs affected
2. **Pull Requests** - Status of PRs with technical context (what problem they solve, approach taken)
3. **Work Items** - Technical tasks and their implementation status
{(options.IncludeBlockers ? "4. **Blockers** - Technical blockers, dependencies waiting, or code review feedback needed" : string.Empty)}
{(options.IncludeNextSteps ? "5. **Next Steps** - Planned technical work, refactoring, or features to implement" : string.Empty)}

Use technical terminology appropriate for developers. Include file names, method names, and specific technical details when relevant.";
    }

    private static string BuildExecutivePrompt(string toneDescription, SummaryOptions options)
    {
        return $@"You are a helpful assistant that summarizes developer work activity for executives and stakeholders.

Generate a {toneDescription} executive summary based on the work data provided.

Focus on BUSINESS VALUE and OUTCOMES, not technical details:
- Features delivered and their business impact
- Progress toward project milestones
- Risks and blockers that might affect timelines
- Resource utilization and team productivity

Structure the update as:
1. **Delivered Value** - What features or capabilities were completed? What business problems do they solve?
2. **In Progress** - What's being worked on? Expected completion timeframes?
3. **Project Health** - Overall status, any concerns about timelines or scope
{(options.IncludeBlockers ? "4. **Risks & Blockers** - What could delay the project? What decisions are needed?" : string.Empty)}
{(options.IncludeNextSteps ? "5. **Upcoming** - What's planned next? Any dependencies on other teams or decisions?" : string.Empty)}

Avoid technical jargon. Translate code changes into business outcomes (e.g., 'fixed login bug' becomes 'improved user authentication reliability').
Keep it high-level and focused on what matters to business stakeholders.";
    }

    private static string BuildCodeReviewPrompt(string toneDescription, SummaryOptions options)
    {
        return $@"You are a senior software engineer reviewing code changes for quality and security.

Generate a {toneDescription} code review summary based on the work data provided.

Focus on CODE QUALITY and SECURITY concerns:
- Potential security vulnerabilities (injection, XSS, authentication issues, etc.)
- Code quality observations (complexity, maintainability, test coverage)
- Architectural concerns or anti-patterns
- Best practices violations
- Areas that need additional review or testing

Structure the update as:
1. **Security Assessment** - Any potential security issues in the changes? Sensitive data handling? Authentication/authorization changes?
2. **Code Quality** - Code complexity, duplication, naming conventions, error handling
3. **Architecture Impact** - Do changes align with architecture? Any concerning patterns?
4. **Test Coverage** - Are changes adequately tested? Missing test scenarios?
{(options.IncludeBlockers ? "5. **Action Items** - Specific issues that should be addressed before merging" : string.Empty)}
{(options.IncludeNextSteps ? "6. **Recommendations** - Suggestions for improvement, refactoring opportunities" : string.Empty)}

Be specific about concerns and provide actionable feedback. Reference specific commits or changes when noting issues.
Prioritize security and reliability concerns over style preferences.";
    }

    private static string BuildUserPrompt(StandupData data)
    {
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        return $@"Here is my work activity data:

**Commits ({data.Commits.Count}):**
{JsonSerializer.Serialize(data.Commits.Select(c => new { c.Message, c.Repository, c.CommittedAt, c.Additions, c.Deletions }), jsonOptions)}

**Open Pull Requests ({data.PullRequests.Count}):**
{JsonSerializer.Serialize(data.PullRequests.Select(pr => new { pr.Title, pr.Repository, pr.Status, pr.IsDraft, pr.ReviewerCount }), jsonOptions)}

**Work Items ({data.WorkItems.Count}):**
{JsonSerializer.Serialize(data.WorkItems.Select(wi => new { wi.Title, wi.Type, wi.Status }), jsonOptions)}

Please generate my standup update.";
    }

    private AzureOpenAIClient CreateClient()
    {
        if (_options.UseAzureIdentity)
        {
            return new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new DefaultAzureCredential());
        }

        return new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey!));
    }
}
