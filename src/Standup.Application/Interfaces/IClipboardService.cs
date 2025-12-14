namespace Standup.Application.Interfaces;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
