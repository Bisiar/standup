// -----------------------------------------------------------------------
// <copyright file="ICrmTenantConfigService.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Standup.Application.Models;

namespace Standup.Application.Interfaces;

/// <summary>
/// Service for managing CRM tenant configurations.
/// </summary>
public interface ICrmTenantConfigService
{
    /// <summary>
    /// Gets all configured CRM tenants.
    /// </summary>
    /// <returns>List of CRM tenant configurations.</returns>
    Task<IReadOnlyList<CrmTenantConfig>> GetAllAsync();

    /// <summary>
    /// Gets a CRM tenant config by ID.
    /// </summary>
    /// <param name="id">The tenant config ID.</param>
    /// <returns>The tenant config, or null if not found.</returns>
    Task<CrmTenantConfig?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds a new CRM tenant configuration.
    /// </summary>
    /// <param name="config">The configuration to add.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddAsync(CrmTenantConfig config);

    /// <summary>
    /// Updates an existing CRM tenant configuration.
    /// </summary>
    /// <param name="config">The configuration to update.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateAsync(CrmTenantConfig config);

    /// <summary>
    /// Deletes a CRM tenant configuration.
    /// </summary>
    /// <param name="id">The tenant config ID to delete.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Tests connectivity to a CRM tenant.
    /// </summary>
    /// <param name="config">The configuration to test.</param>
    /// <returns>True if connection successful, false otherwise with error message.</returns>
    Task<(bool Success, string Message)> TestConnectionAsync(CrmTenantConfig config);
}
