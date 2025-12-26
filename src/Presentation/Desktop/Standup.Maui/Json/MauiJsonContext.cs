using System.Text.Json.Serialization;
using Standup.Application.Models;
using Standup.Domain.Entities;

namespace Standup.Maui.Json;

/// <summary>
/// Source-generated JSON serialization context for trimming-safe serialization.
/// Required to avoid IL2026 warnings when using JsonSerializer with AOT/trimming.
/// </summary>
[JsonSerializable(typeof(List<OrgCredential>))]
[JsonSerializable(typeof(List<RepositoryGroup>))]
[JsonSerializable(typeof(List<ReportHistory>))]
[JsonSerializable(typeof(List<ProjectInstance>))]
[JsonSerializable(typeof(OrgCredential))]
[JsonSerializable(typeof(RepositoryGroup))]
[JsonSerializable(typeof(ReportHistory))]
[JsonSerializable(typeof(ProjectInstance))]
[JsonSourceGenerationOptions(WriteIndented = false)]
public partial class MauiJsonContext : JsonSerializerContext
{
}
