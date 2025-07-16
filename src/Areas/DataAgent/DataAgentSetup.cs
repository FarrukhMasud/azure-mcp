// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Commands;
using AzureMcp.Areas.DataAgent.Services;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.DataAgent;

public class DataAgentSetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDataAgentService, DataAgentService>();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        var dataAgent = new CommandGroup("dataagent", "Data Agent operations - Commands for querying Azure Data Agents in Machine Learning workspaces.");
        rootGroup.AddSubGroup(dataAgent);

        dataAgent.AddCommand("query", new DataAgentQueryCommand(loggerFactory.CreateLogger<DataAgentQueryCommand>()));
        dataAgent.AddCommand("list-workspaces", new DataAgentListWorkspacesCommand(loggerFactory.CreateLogger<DataAgentListWorkspacesCommand>()));
        dataAgent.AddCommand("list", new DataAgentListCommand(loggerFactory.CreateLogger<DataAgentListCommand>()));
        dataAgent.AddCommand("discover", new DataAgentDiscoverCommand(loggerFactory.CreateLogger<DataAgentDiscoverCommand>()));
    }
}
