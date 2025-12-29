// <copyright file="BlazorClipboardService.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Microsoft.JSInterop;
using Standup.Application.Interfaces;

namespace Standup.Blazor.Services;

/// <summary>
/// Blazor JS interop-based implementation of IClipboardService.
/// </summary>
public class BlazorClipboardService : IClipboardService
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorClipboardService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime for interop.</param>
    public BlazorClipboardService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc/>
    public async Task SetTextAsync(string text)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch (JSException)
        {
            // Clipboard API may not be available in all contexts (e.g., non-secure context)
            // Silently fail as clipboard is not critical functionality
        }
    }
}
