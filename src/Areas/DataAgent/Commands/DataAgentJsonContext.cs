// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.DataAgent.Commands;
using AzureMcp.Areas.DataAgent.Models;

namespace AzureMcp.Areas.DataAgent.Commands;

[JsonSerializable(typeof(DataAgentQueryCommand.DataAgentQueryCommandResult))]
[JsonSerializable(typeof(DataAgentListWorkspacesCommand.DataAgentListWorkspacesCommandResult))]
[JsonSerializable(typeof(DataAgentListCommand.DataAgentListCommandResult))]
[JsonSerializable(typeof(DataAgentDiscoverCommand.DataAgentDiscoverCommandResult))]
[JsonSerializable(typeof(WorkspaceInfo))]
[JsonSerializable(typeof(DataAgentInfo))]
[JsonSerializable(typeof(DataAgentQueryResponse))]
[JsonSerializable(typeof(DataAgentQueryRequest))]
[JsonSerializable(typeof(MessageRequest))]
[JsonSerializable(typeof(RunRequest))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal sealed partial class DataAgentJsonContext : JsonSerializerContext;
