using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Standup.Teams.Services;

namespace Standup.Teams.Bots;

public sealed class StandupBot : TeamsActivityHandler
{
    private readonly IStandupApiService _standupService;
    private readonly ICardService _cardService;
    private readonly ILogger<StandupBot> _logger;

    public StandupBot(
        IStandupApiService standupService,
        ICardService cardService,
        ILogger<StandupBot> logger)
    {
        _standupService = standupService;
        _cardService = cardService;
        _logger = logger;
    }

    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var text = turnContext.Activity.Text?.Trim().ToLowerInvariant() ?? string.Empty;

        // Remove bot mention if present
        text = RemoveBotMention(turnContext.Activity, text);

        var response = text switch
        {
            "standup" or "my standup" => await HandleStandupRequestAsync(turnContext, cancellationToken),
            "help" => await HandleHelpRequestAsync(turnContext, cancellationToken),
            "subscribe" => await HandleSubscribeRequestAsync(turnContext, cancellationToken),
            "unsubscribe" => await HandleUnsubscribeRequestAsync(turnContext, cancellationToken),
            "settings" => await HandleSettingsRequestAsync(turnContext, cancellationToken),
            _ => await HandleUnknownCommandAsync(turnContext, cancellationToken)
        };

        await turnContext.SendActivityAsync(response, cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                var welcomeCard = _cardService.CreateWelcomeCard();
                var response = MessageFactory.Attachment(welcomeCard);
                await turnContext.SendActivityAsync(response, cancellationToken);
            }
        }
    }

    protected override async Task<InvokeResponse> OnInvokeActivityAsync(
        ITurnContext<IInvokeActivity> turnContext,
        CancellationToken cancellationToken)
    {
        if (turnContext.Activity.Name == "adaptiveCard/action")
        {
            return await HandleAdaptiveCardActionAsync(turnContext, cancellationToken);
        }

        return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
    }

    private static string RemoveBotMention(IMessageActivity activity, string text)
    {
        if (activity.Entities == null)
        {
            return text;
        }

        foreach (var entity in activity.Entities)
        {
            if (entity.Type == "mention")
            {
                var mention = entity.GetAs<Mention>();
                if (mention?.Text != null)
                {
                    text = text.Replace(mention.Text.ToLowerInvariant(), string.Empty).Trim();
                }
            }
        }

        return text;
    }

    private async Task<IActivity> HandleStandupRequestAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var userId = turnContext.Activity.From.AadObjectId ?? turnContext.Activity.From.Id;

        try
        {
            var standupData = await _standupService.GetStandupAsync(userId, cancellationToken);
            var card = _cardService.CreateStandupCard(standupData);
            return MessageFactory.Attachment(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get standup for user {UserId}", userId);
            return MessageFactory.Text("Sorry, I couldn't retrieve your standup. Please make sure you're registered and have repositories configured.");
        }
    }

    private Task<IActivity> HandleHelpRequestAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var card = _cardService.CreateHelpCard();
        return Task.FromResult<IActivity>(MessageFactory.Attachment(card));
    }

    private async Task<IActivity> HandleSubscribeRequestAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var userId = turnContext.Activity.From.AadObjectId ?? turnContext.Activity.From.Id;
        var conversationId = turnContext.Activity.Conversation.Id;

        try
        {
            await _standupService.SubscribeAsync(userId, conversationId, cancellationToken);
            return MessageFactory.Text("You're now subscribed to daily standup reminders! I'll send you a reminder each workday morning.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe user {UserId}", userId);
            return MessageFactory.Text("Sorry, I couldn't set up your subscription. Please try again later.");
        }
    }

    private async Task<IActivity> HandleUnsubscribeRequestAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var userId = turnContext.Activity.From.AadObjectId ?? turnContext.Activity.From.Id;

        try
        {
            await _standupService.UnsubscribeAsync(userId, cancellationToken);
            return MessageFactory.Text("You've been unsubscribed from daily standup reminders.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe user {UserId}", userId);
            return MessageFactory.Text("Sorry, I couldn't remove your subscription. Please try again later.");
        }
    }

    private Task<IActivity> HandleSettingsRequestAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var card = _cardService.CreateSettingsCard();
        return Task.FromResult<IActivity>(MessageFactory.Attachment(card));
    }

    private Task<IActivity> HandleUnknownCommandAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IActivity>(
            MessageFactory.Text("I didn't understand that. Try saying **standup**, **help**, **subscribe**, or **settings**."));
    }

    private async Task<InvokeResponse> HandleAdaptiveCardActionAsync(
        ITurnContext<IInvokeActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var data = turnContext.Activity.Value as Newtonsoft.Json.Linq.JObject;
        var action = data?["action"]?.ToString();

        return action switch
        {
            "generateStandup" => await HandleGenerateStandupActionAsync(turnContext, cancellationToken),
            "copyToClipboard" => new InvokeResponse { Status = 200 },
            "shareToChannel" => await HandleShareToChannelActionAsync(turnContext, data, cancellationToken),
            _ => new InvokeResponse { Status = 200 }
        };
    }

    private async Task<InvokeResponse> HandleGenerateStandupActionAsync(
        ITurnContext<IInvokeActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var userId = turnContext.Activity.From.AadObjectId ?? turnContext.Activity.From.Id;

        try
        {
            var standupData = await _standupService.GetStandupAsync(userId, cancellationToken);
            var card = _cardService.CreateStandupCard(standupData);

            return new InvokeResponse
            {
                Status = 200,
                Body = new AdaptiveCardInvokeResponse
                {
                    StatusCode = 200,
                    Type = "application/vnd.microsoft.card.adaptive",
                    Value = card.Content
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate standup");
            return new InvokeResponse { Status = 500 };
        }
    }

    private async Task<InvokeResponse> HandleShareToChannelActionAsync(
        ITurnContext<IInvokeActivity> turnContext,
        Newtonsoft.Json.Linq.JObject? data,
        CancellationToken cancellationToken)
    {
        var channelId = data?["channelId"]?.ToString();
        if (string.IsNullOrEmpty(channelId))
        {
            return new InvokeResponse { Status = 400 };
        }

        // Share standup to the specified channel
        var userId = turnContext.Activity.From.AadObjectId ?? turnContext.Activity.From.Id;

        try
        {
            await _standupService.ShareStandupToChannelAsync(userId, channelId, cancellationToken);
            return new InvokeResponse { Status = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share standup to channel");
            return new InvokeResponse { Status = 500 };
        }
    }
}
