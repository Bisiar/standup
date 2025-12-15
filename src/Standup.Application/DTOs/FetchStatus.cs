namespace Standup.Application.DTOs;

/// <summary>
/// Status of a data fetch operation.
/// </summary>
public enum FetchStatus
{
    /// <summary>
    /// Data was fetched successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Data fetch failed with an error.
    /// </summary>
    Error,

    /// <summary>
    /// Data not fetched because no PAT was provided.
    /// </summary>
    NoPat,

    /// <summary>
    /// Data source not applicable for this repository type.
    /// </summary>
    NotApplicable
}
