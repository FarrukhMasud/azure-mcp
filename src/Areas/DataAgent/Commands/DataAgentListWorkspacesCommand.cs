// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Models;
using AzureMcp.Areas.DataAgent.Services;
using AzureMcp.Commands;
using AzureMcp.Options;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.DataAgent.Commands;

public sealed class DataAgentListWorkspacesCommand(ILogger<DataAgentListWorkspacesCommand> logger) : GlobalCommand<GlobalOptions>
{
    private const string CommandTitle = "List Data Agent Workspaces";
    private readonly ILogger<DataAgentListWorkspacesCommand> _logger = logger;

    public override string Name => "list-workspaces";
    public override string Description => "List all available workspaces for data agent discovery";
    public override string Title => CommandTitle;

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            context.Activity?.AddTag("dataagent.operation", "list-workspaces");

            var service = context.GetService<IDataAgentService>();
            var workspaces = await service.ListWorkspacesAsync(options.RetryPolicy, CancellationToken.None);

            var workspacesList = workspaces.ToList();
            _logger.LogInformation("Successfully retrieved {WorkspaceCount} workspaces", workspacesList.Count);

            var result = new DataAgentListWorkspacesCommandResult(workspacesList);
            
            context.Response.Results = ResponseResult.Create(
                result,
                DataAgentJsonContext.Default.DataAgentListWorkspacesCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing list workspaces command");
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record DataAgentListWorkspacesCommandResult(IReadOnlyList<WorkspaceInfo> Workspaces)
    {
        /// <summary>
        /// Gets a formatted summary of workspaces for LLM consumption
        /// </summary>
        public string Summary => $"Found {Workspaces.Count} workspaces available for data agent operations:\n" +
                                string.Join("\n", Workspaces.Select(w => 
                                    $"- {w.DisplayName} (ID: {w.Id}, State: {w.State}" + 
                                    (w.CapacityId != null ? $", Capacity: {w.CapacityId}" : "") + ")"));
    }
}
