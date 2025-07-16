// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Models;
using AzureMcp.Areas.DataAgent.Options;
using AzureMcp.Areas.DataAgent.Services;
using AzureMcp.Commands;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.DataAgent.Commands;

public sealed class DataAgentListCommand(ILogger<DataAgentListCommand> logger) : GlobalCommand<DataAgentListOptions>
{
    private const string CommandTitle = "List Data Agents";
    private readonly ILogger<DataAgentListCommand> _logger = logger;
    private readonly Option<string> _workspaceIdOption = new("--workspace-id", "The ID of the workspace to list data agents from")
    {
        IsRequired = true
    };

    public override string Name => "list";
    public override string Description => "List all available data agents in a workspace for LLM discovery";
    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_workspaceIdOption);
    }

    protected override DataAgentListOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.WorkspaceId = parseResult.GetValueForOption(_workspaceIdOption);
        return options;
    }

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

            context.Activity?.AddTag("dataagent.operation", "list")
                          .AddTag("dataagent.workspace", options.WorkspaceId);

            var service = context.GetService<IDataAgentService>();
            var dataAgents = await service.ListDataAgentsAsync(options.WorkspaceId!, options.RetryPolicy, CancellationToken.None);

            var dataAgentsList = dataAgents.ToList();
            _logger.LogInformation("Successfully retrieved {DataAgentCount} data agents from workspace {WorkspaceId}", dataAgentsList.Count, options.WorkspaceId);

            var result = new DataAgentListCommandResult(dataAgentsList, options.WorkspaceId!);
            
            context.Response.Results = ResponseResult.Create(
                result,
                DataAgentJsonContext.Default.DataAgentListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing list data agents command");
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record DataAgentListCommandResult(IReadOnlyList<DataAgentInfo> DataAgents, string WorkspaceId)
    {
        /// <summary>
        /// Gets a formatted summary of data agents for LLM consumption and selection
        /// </summary>
        public string Summary => $"Found {DataAgents.Count} data agents in workspace {WorkspaceId}:\n" +
                                string.Join("\n", DataAgents.Select(da => 
                                    $"- {da.Summary}"));

        /// <summary>
        /// Gets only active data agents ready for queries
        /// </summary>
        public IReadOnlyList<DataAgentInfo> ActiveDataAgents => DataAgents.Where(da => da.IsActive).ToList();

        /// <summary>
        /// Gets a selection guide for LLM to choose appropriate data agent
        /// </summary>
        public string SelectionGuide => DataAgents.Count > 0 
            ? $"To query a data agent, use the 'query' command with:\n" +
              $"- workspace-id: {WorkspaceId}\n" +
              $"- data-agent-id: [choose from the IDs above]\n" +
              $"- capacity-id: [from workspace capacity]\n" +
              $"- query: [your question]\n" +
              $"Available data agents: {string.Join(", ", ActiveDataAgents.Select(da => $"{da.Name} ({da.Id})"))}"
            : "No data agents found in this workspace.";
    }
}
