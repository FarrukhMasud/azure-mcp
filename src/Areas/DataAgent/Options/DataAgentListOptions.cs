// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Options;

namespace AzureMcp.Areas.DataAgent.Options;

public sealed class DataAgentListOptions : GlobalOptions
{
    public string? WorkspaceId { get; set; }
}
