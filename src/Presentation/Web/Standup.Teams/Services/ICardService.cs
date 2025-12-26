using Microsoft.Bot.Schema;
using Standup.Teams.Models;

namespace Standup.Teams.Services;

public interface ICardService
{
    Attachment CreateWelcomeCard();
    Attachment CreateStandupCard(StandupResponse standup);
    Attachment CreateHelpCard();
    Attachment CreateSettingsCard();
    Attachment CreateReminderCard();
}
