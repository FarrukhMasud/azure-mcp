// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.DataAgent.Models;
using AzureMcp.Options;

namespace AzureMcp.Areas.DataAgent.Services;

public interface IDataAgentService
{
    Task<string> QueryDataAgent(
        string workspaceId,
        string dataAgentId,
        string capacityId,
        string query,
        RetryPolicyOptions? retryPolicy = null);

    Task<string> QueryDataAgentWithStreamingAsync(
        string workspaceId,
        string dataAgentId,
        string capacityId,
        string query,
        RetryPolicyOptions? retryPolicyOptions = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkspaceInfo>> ListWorkspacesAsync(
        RetryPolicyOptions? retryPolicyOptions = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<DataAgentInfo>> ListDataAgentsAsync(
        string workspaceId,
        RetryPolicyOptions? retryPolicyOptions = null,
        CancellationToken cancellationToken = default);
}