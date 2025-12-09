using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Configuration;
using System.Text.Json;

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

    private static string BuildSystemPrompt(SummaryOptions options)
    {
        var toneDescription = options.Tone switch
        {
            SummaryTone.Casual => "casual and friendly, as if talking to coworkers",
            SummaryTone.Brief => "extremely concise, using bullet points only",
            _ => "professional but natural, suitable for a standup meeting"
        };

        var basePrompt = $@"You are a helpful assistant that summarizes developer work activity into standup updates.

Generate a {toneDescription} standup update based on the work data provided.

Structure the update as:
1. **What I completed** - Summarize commits and merged PRs at a high level, focusing on features and fixes rather than technical details
2. **What I'm working on** - Summarize open PRs and in-progress work items
{(options.IncludeBlockers ? "3. **Blockers** - Mention any PRs waiting for review or blocked work items" : "")}
{(options.IncludeNextSteps ? "4. **Next steps** - Brief mention of planned work if evident from the data" : "")}

Keep it natural and conversational - this is meant to be spoken or shared in a team chat.
Focus on impact and outcomes rather than listing every commit.";

        if (!string.IsNullOrEmpty(options.CustomPrompt))
        {
            basePrompt += $"\n\nAdditional instructions: {options.CustomPrompt}";
        }

        return basePrompt;
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
}
