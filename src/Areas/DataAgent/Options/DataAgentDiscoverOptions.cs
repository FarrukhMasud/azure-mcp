// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Options;

namespace AzureMcp.Areas.DataAgent.Commands;

public sealed class DataAgentDiscoverOptions : GlobalOptions
{
    public string? Query { get; set; }
    public string? WorkspaceId { get; set; }
    public string? WorkspaceName { get; set; }
    public string? DataAgentName { get; set; }
}
