// <copyright file="DataProtectionEncryptionService.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.DataProtection;
using Standup.Application.Interfaces;

namespace Standup.Blazor.Services;

/// <summary>
/// ASP.NET Core Data Protection-based implementation of IEncryptionService for Blazor Server.
/// </summary>
public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectionEncryptionService"/> class.
    /// </summary>
    /// <param name="dataProtectionProvider">The data protection provider.</param>
    public DataProtectionEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("Standup.Blazor.Encryption");
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        return _protector.Protect(plainText);
    }

    /// <inheritdoc/>
    public Task<string> EncryptAsync(string plainText)
    {
        return Task.FromResult(Encrypt(plainText));
    }

    /// <inheritdoc/>
    public Task<string> DecryptAsync(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return Task.FromResult(string.Empty);
        }

        try
        {
            var decrypted = _protector.Unprotect(cipherText);
            return Task.FromResult(decrypted);
        }
        catch (Exception)
        {
            // If decryption fails, return empty string
            return Task.FromResult(string.Empty);
        }
    }
}
