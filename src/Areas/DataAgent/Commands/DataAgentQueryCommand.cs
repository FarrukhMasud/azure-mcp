// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Options;
using AzureMcp.Areas.DataAgent.Services;
using AzureMcp.Commands;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.DataAgent.Commands;

public sealed class DataAgentQueryCommand(ILogger<DataAgentQueryCommand> logger) : GlobalCommand<DataAgentQueryOptions>
{
    private const string CommandTitle = "Query Data Agent";
    private readonly ILogger<DataAgentQueryCommand> _logger = logger;
    private readonly Option<string> _workspaceIdOption = DataAgentOptionDefinitions.WorkspaceIdOption;
    private readonly Option<string> _dataAgentIdOption = DataAgentOptionDefinitions.DataAgentIdOption;
    private readonly Option<string> _capacityIdOption = DataAgentOptionDefinitions.CapacityIdOption;
    private readonly Option<string> _queryOption = DataAgentOptionDefinitions.QueryOption;
    private readonly Option<bool> _enableStreamingOption = new("--enable-streaming", "Enable streaming responses for progressive updates");

    public override string Name => "query";
    public override string Description => "Query an Azure Data Agent with optional streaming support";
    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_workspaceIdOption);
        command.AddOption(_dataAgentIdOption);
        command.AddOption(_capacityIdOption);
        command.AddOption(_queryOption);
        command.AddOption(_enableStreamingOption);
    }

    protected override DataAgentQueryOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.WorkspaceId = parseResult.GetValueForOption(_workspaceIdOption);
        options.DataAgentId = parseResult.GetValueForOption(_dataAgentIdOption);
        options.CapacityId = parseResult.GetValueForOption(_capacityIdOption);
        options.Query = parseResult.GetValueForOption(_queryOption);
        options.EnableStreaming = parseResult.GetValueForOption(_enableStreamingOption);
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

            context.Activity?.AddTag("dataagent.workspace", options.WorkspaceId)
                          .AddTag("dataagent.id", options.DataAgentId)
                          .AddTag("dataagent.capacity", options.CapacityId);

            var service = context.GetService<IDataAgentService>();

            string result;
            if (options.EnableStreaming)
            {
                _logger.LogInformation("Executing data agent query with streaming enabled");

                // Create a progress reporter that logs streaming updates
                var progress = new Progress<string>(message =>
                {
                    _logger.LogInformation("Data Agent Progress: {Message}", message);
                    // In a real streaming scenario, these would be sent as MCP progress notifications
                    // The tool loader would handle the actual streaming to the client
                });

                result = await service.QueryDataAgentWithStreamingAsync(
                    options.WorkspaceId!,
                    options.DataAgentId!,
                    options.CapacityId!,
                    options.Query!,
                    options.RetryPolicy,
                    progress,
                    CancellationToken.None);
            }
            else
            {
                _logger.LogInformation("Executing data agent query without streaming");
                result = await service.QueryDataAgent(
                    options.WorkspaceId!,
                    options.DataAgentId!,
                    options.CapacityId!,
                    options.Query!,
                    options.RetryPolicy);
            }

            context.Response.Results = ResponseResult.Create(
                new DataAgentQueryCommandResult(result),
                DataAgentJsonContext.Default.DataAgentQueryCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing data agent query command");
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record DataAgentQueryCommandResult(string QueryResult);
}
