// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.DataAgent.Options;

public static class DataAgentOptionDefinitions
{
    public const string WorkspaceId = "workspace-id";
    public const string DataAgentId = "data-agent-id";
    public const string CapacityId = "capacity-id";
    public const string Query = "query";

    public static readonly Option<string> WorkspaceIdOption = new(
        $"--{WorkspaceId}",
        "The ID of the Azure Machine Learning workspace."
    )
    {
        IsRequired = true
    };

    public static readonly Option<string> DataAgentIdOption = new(
        $"--{DataAgentId}",
        "The ID of the data agent to query."
    )
    {
        IsRequired = true
    };

    public static readonly Option<string> CapacityIdOption = new(
        $"--{CapacityId}",
        "The capacity ID for the data agent query."
    )
    {
        IsRequired = true
    };

    public static readonly Option<string> QueryOption = new(
        $"--{Query}",
        "The query to send to the data agent."
    )
    {
        IsRequired = true
    };
}
