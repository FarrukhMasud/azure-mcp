// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Models;
using AzureMcp.Areas.DataAgent.Services;
using AzureMcp.Commands;
using AzureMcp.Options;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.DataAgent.Commands;

/// <summary>
/// Command for discovering data agents across workspaces with advanced filtering capabilities.
/// Supports filtering by workspace ID, workspace name, and data agent name for optimized performance.
/// This command helps LLMs efficiently identify the most relevant data agents for a given query.
/// </summary>
/// <param name="logger">Logger instance for diagnostics and monitoring</param>
public sealed class DataAgentDiscoverCommand(ILogger<DataAgentDiscoverCommand> logger) : GlobalCommand<DataAgentDiscoverOptions>
{
    private const string CommandTitle = "Discover Data Agents";
    private readonly ILogger<DataAgentDiscoverCommand> _logger = logger;
    private readonly Option<string> _queryOption = new("--query", "The query to help find relevant data agents")
    {
        IsRequired = true
    };
    private readonly Option<string?> _workspaceIdOption = new("--workspace-id", "Optional workspace ID to limit search scope");
    private readonly Option<string?> _workspaceNameOption = new("--workspace-name", "Optional workspace name to limit search scope");
    private readonly Option<string?> _dataAgentNameOption = new("--data-agent-name", "Optional data agent name to filter results");

    public override string Name => "discover";
    public override string Description => "Discover available data agents across workspaces to help LLM select the best one for a query. Supports filtering by workspace ID, workspace name, and data agent name for optimized results.";
    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_queryOption);
        command.AddOption(_workspaceIdOption);
        command.AddOption(_workspaceNameOption);
        command.AddOption(_dataAgentNameOption);
    }

    protected override DataAgentDiscoverOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Query = parseResult.GetValueForOption(_queryOption);
        options.WorkspaceId = parseResult.GetValueForOption(_workspaceIdOption);
        options.WorkspaceName = parseResult.GetValueForOption(_workspaceNameOption);
        options.DataAgentName = parseResult.GetValueForOption(_dataAgentNameOption);
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

            // Add telemetry tags for monitoring and diagnostics
            context.Activity?.AddTag("dataagent.operation", "discover")
                          .AddTag("dataagent.query", options.Query)
                          .AddTag("dataagent.workspace_id", options.WorkspaceId)
                          .AddTag("dataagent.workspace_name", options.WorkspaceName)
                          .AddTag("dataagent.dataagent_name", options.DataAgentName);

            var service = context.GetService<IDataAgentService>();
            
            // Step 1: Get filtered workspaces based on provided criteria
            var workspaces = await GetFilteredWorkspacesAsync(service, options);
            var workspacesList = workspaces.ToList();
            
            _logger.LogInformation("Searching across {WorkspaceCount} workspaces for data agents", workspacesList.Count);

            // Step 2: Get data agents from filtered workspaces with optimized processing
            var allDataAgents = await GetDataAgentsFromWorkspacesAsync(service, workspacesList, options);

            _logger.LogInformation("Found {DataAgentCount} data agents across {WorkspaceCount} workspaces", 
                allDataAgents.Count, workspacesList.Count);

            var result = new DataAgentDiscoverCommandResult(allDataAgents, options.Query!, options.WorkspaceId);
            
            context.Response.Results = ResponseResult.Create(
                result,
                DataAgentJsonContext.Default.DataAgentDiscoverCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing discover data agents command");
            HandleException(context, ex);
        }

        return context.Response;
    }

    /// <summary>
    /// Gets filtered workspaces based on the provided criteria for optimized performance
    /// </summary>
    /// <param name="service">The data agent service</param>
    /// <param name="options">The discover options containing filter criteria</param>
    /// <returns>Filtered workspaces matching the criteria</returns>
    private async Task<IEnumerable<WorkspaceInfo>> GetFilteredWorkspacesAsync(
        IDataAgentService service, 
        DataAgentDiscoverOptions options)
    {
        // Get all workspaces first
        var allWorkspaces = await service.ListWorkspacesAsync(options.RetryPolicy, CancellationToken.None);
        
        // Apply workspace filters in sequence for optimal performance
        var filteredWorkspaces = allWorkspaces.Where(w => w.IsActive);
        
        // Filter by workspace ID if provided (exact match)
        if (!string.IsNullOrEmpty(options.WorkspaceId))
        {
            filteredWorkspaces = filteredWorkspaces.Where(w => 
                w.Id.Equals(options.WorkspaceId, StringComparison.OrdinalIgnoreCase));
        }
        
        // Filter by workspace name if provided (case-insensitive partial match)
        if (!string.IsNullOrEmpty(options.WorkspaceName))
        {
            filteredWorkspaces = filteredWorkspaces.Where(w =>
                w.Name.Contains(options.WorkspaceName, StringComparison.OrdinalIgnoreCase) ||
                w.DisplayName.Contains(options.WorkspaceName, StringComparison.OrdinalIgnoreCase));
        }
        
        return filteredWorkspaces;
    }

    /// <summary>
    /// Gets data agents from the provided workspaces with optimized filtering
    /// </summary>
    /// <param name="service">The data agent service</param>
    /// <param name="workspaces">The workspaces to search in</param>
    /// <param name="options">The discover options containing filter criteria</param>
    /// <returns>Filtered data agents from the workspaces</returns>
    private async Task<List<(WorkspaceInfo workspace, DataAgentInfo dataAgent)>> GetDataAgentsFromWorkspacesAsync(
        IDataAgentService service,
        List<WorkspaceInfo> workspaces,
        DataAgentDiscoverOptions options)
    {
        var allDataAgents = new List<(WorkspaceInfo workspace, DataAgentInfo dataAgent)>();

        // Process workspaces in parallel for better performance when dealing with multiple workspaces
        var tasks = workspaces.Select(async workspace =>
        {
            try
            {
                var dataAgents = await service.ListDataAgentsAsync(workspace.Id, options.RetryPolicy, CancellationToken.None);
                
                // Filter data agents: only active ones and apply name filter if specified
                var filteredDataAgents = dataAgents.Where(da => da.IsActive);
                
                // Apply data agent name filter if provided (case-insensitive partial match)
                if (!string.IsNullOrEmpty(options.DataAgentName))
                {
                    filteredDataAgents = filteredDataAgents.Where(da =>
                        da.Name.Contains(options.DataAgentName, StringComparison.OrdinalIgnoreCase) ||
                        da.DisplayName.Contains(options.DataAgentName, StringComparison.OrdinalIgnoreCase) ||
                        (da.Description?.Contains(options.DataAgentName, StringComparison.OrdinalIgnoreCase) ?? false));
                }
                
                return filteredDataAgents.Select(da => (workspace, da)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list data agents from workspace {WorkspaceId}: {ErrorMessage}", 
                    workspace.Id, ex.Message);
                return new List<(WorkspaceInfo workspace, DataAgentInfo dataAgent)>();
            }
        });

        // Wait for all tasks to complete and aggregate results
        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            allDataAgents.AddRange(result);
        }

        return allDataAgents;
    }

    internal record DataAgentDiscoverCommandResult(
        IReadOnlyList<(WorkspaceInfo workspace, DataAgentInfo dataAgent)> DataAgents, 
        string Query,
        string? WorkspaceId)
    {
        /// <summary>
        /// Gets a comprehensive discovery summary for LLM consumption
        /// </summary>
        public string DiscoverySummary => $"Query: \"{Query}\"\n" +
                                         $"Scope: {(WorkspaceId != null ? $"Workspace {WorkspaceId}" : "All workspaces")}\n" +
                                         $"Found {DataAgents.Count} available data agents:\n\n" +
                                         string.Join("\n", DataAgents.Select(da => 
                                             $"📊 {da.dataAgent.DisplayName}\n" +
                                             $"   - ID: {da.dataAgent.Id}\n" +
                                             $"   - Workspace: {da.workspace.DisplayName} ({da.workspace.Id})\n" +
                                             $"   - Type: {da.dataAgent.Type}\n" +
                                             $"   - Capacity: {da.workspace.CapacityId}\n" +
                                             $"   - Tags: {(da.dataAgent.Tags?.Length > 0 ? string.Join(", ", da.dataAgent.Tags) : "None")}\n"));

        /// <summary>
        /// Gets selection guidance for LLM to execute queries
        /// </summary>
        public string SelectionGuide => DataAgents.Count > 0 
            ? $"\nTo query any of these data agents, use:\n" +
              $"azmcp dataagent query --workspace-id <workspace-id> --data-agent-id <data-agent-id> --capacity-id <capacity-id> --query \"<your-question>\"\n\n" +
              $"Example commands:\n" +
              string.Join("\n", DataAgents.Take(3).Select(da => 
                  $"azmcp dataagent query --workspace-id {da.workspace.Id} --data-agent-id {da.dataAgent.Id} --capacity-id {da.workspace.CapacityId} --query \"{Query}\""))
            : "No data agents found. Please check if workspaces contain data agents or try different filter criteria.";

        /// <summary>
        /// Gets the workspaces that contain data agents
        /// </summary>
        public IReadOnlyList<WorkspaceInfo> WorkspacesWithDataAgents => DataAgents
            .Select(da => da.workspace)
            .Distinct()
            .ToList();

        /// <summary>
        /// Gets suggested data agents based on query matching against names, descriptions, and tags
        /// Uses case-insensitive search for better user experience
        /// </summary>
        public IReadOnlyList<(WorkspaceInfo workspace, DataAgentInfo dataAgent)> SuggestedDataAgents => DataAgents
            .Where(da => 
                da.dataAgent.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                da.dataAgent.DisplayName.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                da.dataAgent.Description?.Contains(Query, StringComparison.OrdinalIgnoreCase) == true ||
                da.dataAgent.Tags?.Any(tag => tag.Contains(Query, StringComparison.OrdinalIgnoreCase)) == true)
            .OrderByDescending(da => CalculateRelevanceScore(da.dataAgent, Query))
            .ToList();

        /// <summary>
        /// Calculates relevance score for data agent based on query matching
        /// Higher score indicates better match
        /// </summary>
        private static int CalculateRelevanceScore(DataAgentInfo dataAgent, string query)
        {
            var score = 0;
            var queryLower = query.ToLowerInvariant();
            
            // Exact name match gets highest score
            if (dataAgent.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
                score += 100;
            
            // Name contains query
            if (dataAgent.Name.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                score += 50;
            
            // Description contains query
            if (dataAgent.Description?.Contains(queryLower, StringComparison.OrdinalIgnoreCase) == true)
                score += 30;
            
            // Tags contain query
            if (dataAgent.Tags?.Any(tag => tag.Contains(queryLower, StringComparison.OrdinalIgnoreCase)) == true)
                score += 20;
            
            return score;
        }
    }
}
