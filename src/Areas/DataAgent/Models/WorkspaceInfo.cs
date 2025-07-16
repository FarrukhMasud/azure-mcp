// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.DataAgent.Models;

/// <summary>
/// Represents workspace information for data agent discovery
/// </summary>
public sealed record WorkspaceInfo(
    string Id,
    string Name,
    string? Description,
    string? CapacityId,
    string Type,
    string State)
{
    /// <summary>
    /// Gets a formatted display name for the workspace
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Description) ? Name : $"{Name} - {Description}";

    /// <summary>
    /// Indicates if the workspace is active and available for data agent operations
    /// </summary>
    public bool IsActive => State.Equals("Active", StringComparison.OrdinalIgnoreCase);
}
