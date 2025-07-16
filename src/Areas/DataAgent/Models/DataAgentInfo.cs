// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.DataAgent.Models;

/// <summary>
/// Represents data agent information for discovery and selection
/// </summary>
public sealed record DataAgentInfo(
    string Id,
    string Name,
    string? Description,
    string WorkspaceId,
    string Type,
    string State,
    DateTime CreatedDate,
    DateTime ModifiedDate,
    string[]? Tags)
{
    /// <summary>
    /// Gets a formatted display name for the data agent
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Description) ? Name : $"{Name} - {Description}";

    /// <summary>
    /// Indicates if the data agent is active and available for queries
    /// </summary>
    public bool IsActive => State.Equals("Active", StringComparison.OrdinalIgnoreCase) || 
                           State.Equals("Ready", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a summary of the data agent's capabilities for LLM selection
    /// </summary>
    public string Summary => $"Data Agent: {DisplayName} (ID: {Id}) - Type: {Type}, State: {State}" + 
                            (Tags?.Length > 0 ? $", Tags: {string.Join(", ", Tags)}" : string.Empty);
}
