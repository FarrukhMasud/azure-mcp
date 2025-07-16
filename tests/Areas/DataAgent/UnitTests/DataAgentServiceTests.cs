// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Services;
using AzureMcp.Services.Azure.Tenant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzureMcp.Tests.Areas.DataAgent.UnitTests;

[Trait("Area", "DataAgent")]
public class DataAgentServiceTests
{
    private readonly ITenantService _tenantService;
    private readonly DataAgentService _service;

    public DataAgentServiceTests()
    {
        _tenantService = Substitute.For<ITenantService>();
        _service = new DataAgentService(_tenantService, new Logger<DataAgentService>(new LoggerFactory()));
    }

    [Fact]
    public async Task QueryDataAgent_ThrowsArgumentException_WhenWorkspaceNameIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.QueryDataAgent(null!, "agent-id", "capacity-id", "query"));
    }

    [Fact]
    public async Task QueryDataAgent_ThrowsArgumentException_WhenDataAgentIdIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.QueryDataAgent("workspace", null!, "capacity-id", "query"));
    }

    [Fact]
    public async Task QueryDataAgent_ThrowsArgumentException_WhenCapacityIdIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.QueryDataAgent("workspace", "agent-id", null!, "query"));
    }

    [Fact]
    public async Task QueryDataAgent_ThrowsArgumentException_WhenQueryIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.QueryDataAgent("workspace", "agent-id", "capacity-id", null!));
    }

    [Fact]
    public async Task QueryDataAgent_ThrowsArgumentException_WhenWorkspaceNameIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.QueryDataAgent("", "agent-id", "capacity-id", "query"));
    }
}
