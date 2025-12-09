using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;

namespace Standup.Teams;

public sealed class AdapterWithErrorHandler : CloudAdapter
{
    public AdapterWithErrorHandler(
        BotFrameworkAuthentication auth,
        ILogger<AdapterWithErrorHandler> logger)
        : base(auth, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "Unhandled error: {Message}", exception.Message);

            await turnContext.SendActivityAsync("Sorry, something went wrong. Please try again.");

            await turnContext.TraceActivityAsync(
                "OnTurnError Trace",
                exception.Message,
                "https://www.botframework.com/schemas/error",
                "TurnError");
        };
    }
}
