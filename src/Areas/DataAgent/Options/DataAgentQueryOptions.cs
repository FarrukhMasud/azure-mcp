// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Options;

namespace AzureMcp.Areas.DataAgent.Options;

public class DataAgentQueryOptions : GlobalOptions
{
    [JsonPropertyName(DataAgentOptionDefinitions.WorkspaceId)]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName(DataAgentOptionDefinitions.DataAgentId)]
    public string? DataAgentId { get; set; }

    [JsonPropertyName(DataAgentOptionDefinitions.CapacityId)]
    public string? CapacityId { get; set; }

    [JsonPropertyName(DataAgentOptionDefinitions.Query)]
    public string? Query { get; set; }

    [JsonPropertyName("enableStreaming")]
    public bool EnableStreaming { get; set; }
}
