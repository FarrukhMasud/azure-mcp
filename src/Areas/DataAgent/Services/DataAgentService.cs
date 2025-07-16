// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Azure.Core;
using AzureMcp.Areas.DataAgent.Commands;
using AzureMcp.Areas.DataAgent.Models;
using AzureMcp.Options;
using AzureMcp.Services.Azure;
using AzureMcp.Services.Azure.Tenant;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.DataAgent.Services;

public class DataAgentService(ITenantService tenantService, ILogger<DataAgentService> logger) : BaseAzureService(tenantService), IDataAgentService
{
    private const string DataAgentApiVersion = "2023-02-01-preview";
    private readonly ILogger<DataAgentService> _logger = logger;

    public async Task<string> QueryDataAgent(
        string workspaceId,
        string dataAgentId,
        string capacityId,
        string query,
        RetryPolicyOptions? retryPolicy = null)
    {
        _logger.LogInformation("Starting QueryDataAgent - WorkspaceId: {WorkspaceId}, DataAgentId: {DataAgentId}, CapacityId: {CapacityId}, Query: {Query}", 
            workspaceId, dataAgentId, capacityId, query);

        ArgumentException.ThrowIfNullOrEmpty(workspaceId);
        ArgumentException.ThrowIfNullOrEmpty(dataAgentId);
        ArgumentException.ThrowIfNullOrEmpty(capacityId);
        ArgumentException.ThrowIfNullOrEmpty(query);

        _logger.LogDebug("Input validation passed for all required parameters");

        try
        {
            _logger.LogDebug("Getting credential from base service");
            var credential = await GetCredential();
            
            _logger.LogDebug("Requesting access token for ML scope");
            var accessToken = await credential.GetTokenAsync(
                new TokenRequestContext(["https://analysis.windows.net/powerbi/api/.default"]),
                CancellationToken.None);

            _logger.LogInformation("Successfully obtained access token for ML scope: {TokenType}", accessToken.TokenType);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            httpClient.DefaultRequestHeaders.Add("x-ms-ai-assistant-agent", "ai_skill");
            httpClient.DefaultRequestHeaders.Add("x-ms-ai-aiskill-stage", "sandbox");
            httpClient.DefaultRequestHeaders.Add("x-ms-root-activity-id", Guid.NewGuid().ToString());

            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            var urlPrefix = $"http://localhost:38080/webapi/capacities/{capacityId}/workloads/ML/AISkill/Automatic/v1/workspaces/{workspaceId}/artifacts/{dataAgentId}/aiassistant/openai";
            _logger.LogInformation("Constructed URL prefix: {UrlPrefix}", urlPrefix);

            // Create Assistant
            _logger.LogDebug("Creating assistant...");
            var assistantId = await CreateAssitantAsync(httpClient, urlPrefix);
            _logger.LogInformation("Successfully created assistant with ID: {AssistantId}", assistantId);
            
            // Create Thread
            _logger.LogDebug("Creating thread...");
            var threadId = await CreateThreadAsync(httpClient, urlPrefix);
            _logger.LogInformation("Successfully created thread with ID: {ThreadId}", threadId);
            
            // Create Message
            _logger.LogDebug("Creating message in thread {ThreadId} with query: {Query}", threadId, query);
            await CreateMessageAsync(httpClient, urlPrefix, threadId, query);
            _logger.LogInformation("Successfully created message in thread {ThreadId}", threadId);
            
            // Create Run
            _logger.LogDebug("Creating run with assistant {AssistantId} in thread {ThreadId}", assistantId, threadId);
            var runResponse = await CreateRunAsync(httpClient, urlPrefix, threadId, assistantId);
            _logger.LogInformation("Successfully created run. Response: {RunResponse}", runResponse);
            
            // Get Messages 
            _logger.LogDebug("Retrieving messages from thread {ThreadId}", threadId);
            var messages = await GetMessages(httpClient, urlPrefix, threadId);
            _logger.LogInformation("Successfully retrieved {MessageCount} messages from thread {ThreadId}", messages.Count(), threadId);

            
            var result = string.Join(Environment.NewLine, messages);
            _logger.LogInformation("QueryDataAgent completed successfully. Result length: {ResultLength} characters", result.Length);
            _logger.LogDebug("QueryDataAgent result: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query data agent '{DataAgentId}' in workspace '{WorkspaceId}' with capacity '{CapacityId}': {ErrorMessage}", 
                dataAgentId, workspaceId, capacityId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Query a data agent with streaming support for progressive results
    /// </summary>
    public async Task<string> QueryDataAgentWithStreamingAsync(
        string workspaceId,
        string dataAgentId,
        string capacityId,
        string query,
        RetryPolicyOptions? retryPolicyOptions = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId, nameof(workspaceId));
        ArgumentException.ThrowIfNullOrWhiteSpace(dataAgentId, nameof(dataAgentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(capacityId, nameof(capacityId));
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        _logger.LogInformation("Starting QueryDataAgentWithStreamingAsync for DataAgent: {DataAgentId}, Workspace: {WorkspaceId}, Capacity: {CapacityId}, Query: {Query}", 
            dataAgentId, workspaceId, capacityId, query);

        try
        {
            _logger.LogDebug("Getting credential from base service");
            var credential = await GetCredential();
            
            _logger.LogDebug("Requesting access token for ML scope");
            var accessToken = await credential.GetTokenAsync(
                new TokenRequestContext(["https://analysis.windows.net/powerbi/api/.default"]),
                CancellationToken.None);
            Console.WriteLine($"Successfully obtained access token for ML scope: {accessToken.TokenType}");
            _logger.LogInformation("Successfully obtained access token for ML scope");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            httpClient.DefaultRequestHeaders.Add("x-ms-fabric-capacity-id", capacityId);

            // Report progress
            progress?.Report("Initializing data agent query...");

            var urlPrefix = $"http://localhost:38080/v1/workspaces/{workspaceId}/dataagents/{dataAgentId}";
            _logger.LogInformation("Constructed URL prefix: {UrlPrefix}", urlPrefix);

            // Create Assistant
            progress?.Report("Creating assistant...");
            _logger.LogDebug("Creating assistant...");
            var assistantId = await CreateAssitantAsync(httpClient, urlPrefix);
            _logger.LogInformation("Successfully created assistant with ID: {AssistantId}", assistantId);
            
            // Create Thread
            progress?.Report("Creating thread...");
            _logger.LogDebug("Creating thread...");
            var threadId = await CreateThreadAsync(httpClient, urlPrefix);
            _logger.LogInformation("Successfully created thread with ID: {ThreadId}", threadId);
            
            // Create Message
            progress?.Report("Creating message in thread...");
            _logger.LogDebug("Creating message in thread {ThreadId} with query: {Query}", threadId, query);
            await CreateMessageAsync(httpClient, urlPrefix, threadId, query);
            _logger.LogInformation("Successfully created message in thread {ThreadId}", threadId);
            
            // Create Run with streaming
            progress?.Report("Starting data agent run...");
            _logger.LogDebug("Creating run with assistant {AssistantId} in thread {ThreadId}", assistantId, threadId);
            var runResponse = await CreateRunWithStreamingAsync(httpClient, urlPrefix, threadId, assistantId, progress, cancellationToken);
            _logger.LogInformation("Successfully created run. Response: {RunResponse}", runResponse);
            
            // Get Messages 
            progress?.Report("Retrieving final results...");
            _logger.LogDebug("Retrieving messages from thread {ThreadId}", threadId);
            var messages = await GetMessages(httpClient, urlPrefix, threadId);
            _logger.LogInformation("Successfully retrieved {MessageCount} messages from thread {ThreadId}", messages.Count(), threadId);

            // return the response from the run
            var result = string.Join(Environment.NewLine, messages);
            progress?.Report("Query completed successfully.");
            _logger.LogInformation("QueryDataAgentWithStreamingAsync completed successfully. Result length: {ResultLength} characters", result.Length);
            _logger.LogDebug("QueryDataAgentWithStreamingAsync result: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error: {ex.Message}");
            _logger.LogError(ex, "Failed to query data agent '{DataAgentId}' in workspace '{WorkspaceId}' with capacity '{CapacityId}': {ErrorMessage}", 
                dataAgentId, workspaceId, capacityId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a run with streaming support for progressive updates
    /// </summary>
    protected virtual async Task<string> CreateRunWithStreamingAsync(
        HttpClient httpClient, 
        string uriPrefix, 
        string threadId, 
        string assistantId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating run in thread {ThreadId} with assistant {AssistantId}", threadId, assistantId);
        
        var runUri = new Uri($"{uriPrefix}/threads/{threadId}/runs?api-version=2024-07-01-preview");
        _logger.LogDebug("Run creation URI: {RunUri}", runUri);

        var runRequest = new RunRequest
        {
            AssistantId = assistantId,
            Stream = true, // Enable streaming responses
        };

        _logger.LogDebug("Created run request with AssistantId: {AssistantId}, Stream: {Stream}", runRequest.AssistantId, runRequest.Stream);

        var runJson = JsonSerializer.Serialize(runRequest, DataAgentJsonContext.Default.RunRequest);
        _logger.LogDebug("Serialized run JSON: {RunJson}", runJson);
        
        // Make streaming request
        return await MakeStreamingPostRequestAsync(httpClient, runUri, runJson, progress, cancellationToken);
    }

    /// <summary>
    /// Makes a POST request with streaming support for Server-Sent Events
    /// </summary>
    protected async Task<string> MakeStreamingPostRequestAsync(
        HttpClient httpClient,
        Uri uri,
        string content,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Making streaming POST request to {Uri} with content: {Content}", uri, content);
        
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        // Set headers for streaming
        request.Headers.Add("Accept", "text/event-stream");
        request.Headers.Add("Cache-Control", "no-cache");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBuilder = new StringBuilder();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Parse SSE format: data: {...}
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6); // Remove "data: " prefix
                
                if (data != "[DONE]") // Common SSE completion signal
                {
                    responseBuilder.AppendLine(data);
                    
                    // Report progress with partial data
                    progress?.Report($"Received: {data}");
                    
                    _logger.LogDebug("Streaming data received: {Data}", data);
                }
            }
        }

        var fullResponse = responseBuilder.ToString();
        _logger.LogDebug("Full streaming response: {Response}", fullResponse);
        
        return fullResponse;
    }

    protected async Task<string> MakePostRequestAsync(
        HttpClient httpClient,
        Uri uri,
        string content,
        bool returnFullReponse = false,
        string idField = "id")
    {
        _logger.LogDebug("Making POST request to {Uri} with content: {Content}", uri, content);
        
        var response = await httpClient.PostAsync(uri, new StringContent(content, Encoding.UTF8, "application/json"));
        
        _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
        
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        _logger.LogDebug("Response body: {ResponseBody}", responseBody);

        if (returnFullReponse)
        {
            _logger.LogDebug("Returning full response as requested");
            return responseBody;
        }

        var jsonDocument = JsonDocument.Parse(responseBody);
        var id = jsonDocument.RootElement.GetProperty(idField).GetString();
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogError("Failed to extract {IdField} from response: {ResponseBody}", idField, responseBody);
            throw new Exception($"Failed to get {idField} from response.");
        }

        _logger.LogDebug("Successfully extracted {IdField}: {Id}", idField, id);
        return id;
    }


    /// <summary>
    /// Creates an assistant using the specified HTTP client and URI prefix.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="uriPrefix">URI prefix.</param>
    /// <returns>Assistant Id.</returns>
    protected virtual async Task<string> CreateAssitantAsync(HttpClient httpClient, string uriPrefix)
    {
        _logger.LogDebug("Creating assistant with URI prefix: {UriPrefix}", uriPrefix);
        
        var assistantUri = new Uri(
            $"{uriPrefix}/assistants?api-version=2024-05-01-preview");
        
        _logger.LogDebug("Assistant creation URI: {AssistantUri}", assistantUri);

        var assistantId = await MakePostRequestAsync(httpClient, assistantUri, string.Empty);
        
        _logger.LogInformation("Successfully created assistant with ID: {AssistantId}", assistantId);
        
        return assistantId;
    }

    /// <summary>
    /// Creates a thread using the specified HTTP client and URI prefix.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="uriPrefix">URI prefix.</param>
    /// <returns>Thread Id.</returns>
    protected virtual async Task<string> CreateThreadAsync(HttpClient httpClient, string uriPrefix)
    {
        _logger.LogDebug("Creating thread with URI prefix: {UriPrefix}", uriPrefix);
        
        var threadUri = new Uri(
            $"{uriPrefix}/threads?api-version=2024-05-01-preview");
        
        _logger.LogDebug("Thread creation URI: {ThreadUri}", threadUri);

        var threadId = await MakePostRequestAsync(httpClient, threadUri, string.Empty);
        
        _logger.LogInformation("Successfully created thread with ID: {ThreadId}", threadId);
        
        return threadId;
    }

    /// <summary>
    /// Deletes a thread using the specified HTTP client and URI prefix.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="uriPrefix">URI prefix.</param>
    /// <param name="threadId">Thread Id.</param>
    /// <returns>Task for async operations.</returns>
    protected virtual async Task DeleteThreadAsync(HttpClient httpClient, string uriPrefix, string threadId)
    {
        _logger.LogDebug("Deleting thread {ThreadId} with URI prefix: {UriPrefix}", threadId, uriPrefix);
        
        var deleteThreadUri = new Uri(
               $"{uriPrefix}/threads/{threadId}?api-version=2024-07-01-preview");
        
        _logger.LogDebug("Thread deletion URI: {DeleteThreadUri}", deleteThreadUri);
        
        var deleteResponse = await httpClient.DeleteAsync(deleteThreadUri);
        
        _logger.LogDebug("Thread deletion response status: {StatusCode}", deleteResponse.StatusCode);
        
        deleteResponse.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Successfully deleted thread {ThreadId}", threadId);
    }

    /// <summary>
    /// Creates a message in the specified thread using the HTTP client and URI prefix.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="uriPrefix">URI prefix.</param>
    /// <param name="threadId">Thread Id.</param>
    /// <param name="prompt">Prompt.</param>
    /// <returns>Task for async operations.</returns>
    protected virtual Task<string> CreateMessageAsync(HttpClient httpClient, string uriPrefix, string threadId, string prompt)
    {
        _logger.LogDebug("Creating message in thread {ThreadId} with prompt: {Prompt}", threadId, prompt);
        
        // Build the URI for creating a message in the thread
        var messageUri = new Uri($"{uriPrefix}/threads/{threadId}/messages?api-version=2024-07-01-preview");
        
        _logger.LogDebug("Message creation URI: {MessageUri}", messageUri);

        // Create a message request with user content asking for stock data
        var messageRequest = new MessageRequest
        {
            Role = "user",
            Content = prompt,
        };

        _logger.LogDebug("Created message request with role: {Role}, content: {Content}", messageRequest.Role, messageRequest.Content);

        // Serialize and send the message request
        var messageJson = JsonSerializer.Serialize(messageRequest, DataAgentJsonContext.Default.MessageRequest);
        
        _logger.LogDebug("Serialized message JSON: {MessageJson}", messageJson);
        
        return MakePostRequestAsync(httpClient, messageUri, messageJson);
    }

    /// <summary>
    /// Creates a run in the specified thread using the HTTP client and URI prefix.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="uriPrefix">URI prefix.</param>
    /// <param name="threadId">Thread Id.</param>
    /// <param name="assistantId">Assistant Id.</param>
    /// <returns>Task for async operations.</returns>
    protected virtual Task<string> CreateRunAsync(HttpClient httpClient, string uriPrefix, string threadId, string assistantId)
    {
        _logger.LogDebug("Creating run in thread {ThreadId} with assistant {AssistantId}", threadId, assistantId);
        
        // Build the URI for creating a run in the thread
        var runUri = new Uri($"{uriPrefix}/threads/{threadId}/runs?api-version=2024-07-01-preview");
        
        _logger.LogDebug("Run creation URI: {RunUri}", runUri);

        // Create a run request to process messages with streaming enabled
        var runRequest = new RunRequest
        {
            AssistantId = assistantId,
            Stream = true, // Enable streaming responses
        };

        _logger.LogDebug("Created run request with AssistantId: {AssistantId}, Stream: {Stream}", runRequest.AssistantId, runRequest.Stream);

        // Serialize and send the run request
        var runJson = JsonSerializer.Serialize(runRequest, DataAgentJsonContext.Default.RunRequest);
        
        _logger.LogDebug("Serialized run JSON: {RunJson}", runJson);
        
        return MakePostRequestAsync(httpClient, runUri, runJson, true);
    }

    /// <summary>
    /// Retrieves messages from the specified thread using the HTTP client and URI prefix.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="uriPrefix">URI prefix.</param>
    /// <param name="threadId">Thread Id.</param>
    /// <returns>Messages.</returns>
    protected virtual async Task<IEnumerable<string>> GetMessages(HttpClient httpClient, string uriPrefix, string threadId)
    {
        _logger.LogDebug("Retrieving messages from thread {ThreadId}", threadId);
        
        // Build the URI for getting messages from the thread
        var messagesUri = new Uri($"{uriPrefix}/threads/{threadId}/messages?api-version=2024-07-01-preview");
        
        _logger.LogDebug("Messages retrieval URI: {MessagesUri}", messagesUri);

        // Get all messages from the thread
        var messagesResponse = await httpClient.GetAsync(messagesUri);
        
        _logger.LogDebug("Messages response status: {StatusCode}", messagesResponse.StatusCode);
        
        messagesResponse.EnsureSuccessStatusCode();

        // Parse the response content
        var messagesContent = await messagesResponse.Content.ReadAsStringAsync();
        
        _logger.LogDebug("Raw messages content: {MessagesContent}", messagesContent);
        
        var messagesData = JsonDocument.Parse(messagesContent);

        // Extract the messages array from the response
        var messages = messagesData.RootElement.GetProperty("data").EnumerateArray();
        var messageList = new List<string>();
        
        var messageCount = 0;
        foreach (var message in messages)
        {
            var messageString = message.ToString();
            messageList.Add(messageString);
            messageCount++;
            _logger.LogDebug("Message {MessageIndex}: {Message}", messageCount, messageString);
        }

        _logger.LogInformation("Successfully retrieved {MessageCount} messages from thread {ThreadId}", messageCount, threadId);
        
        return messageList;
    }

    /// <summary>
    /// Lists all available workspaces for data agent discovery
    /// </summary>
    public async Task<IEnumerable<WorkspaceInfo>> ListWorkspacesAsync(
        RetryPolicyOptions? retryPolicyOptions = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ListWorkspacesAsync");

        try
        {
            _logger.LogDebug("Getting credential from base service");
            var credential = await GetCredential();
            
            _logger.LogDebug("Requesting access token for Fabric API scope");
            var accessToken = await credential.GetTokenAsync(
                new TokenRequestContext(["https://analysis.windows.net/powerbi/api/.default"]),
                CancellationToken.None);
            
            _logger.LogInformation("Successfully obtained access token for Fabric API scope");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            var workspacesUri = new Uri("https://dailyapi.fabric.microsoft.com/v1/workspaces");
            _logger.LogDebug("Workspaces API URI: {WorkspacesUri}", workspacesUri);

            var response = await httpClient.GetAsync(workspacesUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Workspaces API response: {Response}", responseContent);

            var jsonDocument = JsonDocument.Parse(responseContent);
            var workspaces = new List<WorkspaceInfo>();

            if (jsonDocument.RootElement.TryGetProperty("value", out var workspacesArray))
            {
                foreach (var workspace in workspacesArray.EnumerateArray())
                {
                    var workspaceInfo = new WorkspaceInfo(
                        Id: workspace.GetProperty("id").GetString() ?? string.Empty,
                        Name: workspace.GetProperty("displayName").GetString() ?? string.Empty,
                        Description: workspace.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        CapacityId: workspace.TryGetProperty("capacityId", out var capacity) ? capacity.GetString() : null,
                        Type: workspace.TryGetProperty("type", out var type) ? type.GetString() ?? "Workspace" : "Workspace",
                        State: workspace.TryGetProperty("state", out var state) ? state.GetString() ?? "Active" : "Active"
                    );
                    workspaces.Add(workspaceInfo);
                }
            }

            _logger.LogInformation("Successfully retrieved {WorkspaceCount} workspaces", workspaces.Count);
            return workspaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list workspaces: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Lists all available data agents in a specific workspace for LLM discovery
    /// </summary>
    public async Task<IEnumerable<DataAgentInfo>> ListDataAgentsAsync(
        string workspaceId,
        RetryPolicyOptions? retryPolicyOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId, nameof(workspaceId));

        _logger.LogInformation("Starting ListDataAgentsAsync for workspace: {WorkspaceId}", workspaceId);

        try
        {
            _logger.LogDebug("Getting credential from base service");
            var credential = await GetCredential();
            
            _logger.LogDebug("Requesting access token for Fabric API scope");
            var accessToken = await credential.GetTokenAsync(
                new TokenRequestContext(["https://analysis.windows.net/powerbi/api/.default"]),
                CancellationToken.None);
            
            _logger.LogInformation("Successfully obtained access token for Fabric API scope");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            // List all items in the workspace and filter for data agents
            var itemsUri = new Uri($"https://dailyapi.fabric.microsoft.com/v1/workspaces/{workspaceId}/items");
            _logger.LogDebug("Items API URI: {ItemsUri}", itemsUri);

            var response = await httpClient.GetAsync(itemsUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Items API response: {Response}", responseContent);

            var jsonDocument = JsonDocument.Parse(responseContent);
            var dataAgents = new List<DataAgentInfo>();

            if (jsonDocument.RootElement.TryGetProperty("value", out var itemsArray))
            {
                foreach (var item in itemsArray.EnumerateArray())
                {
                    var itemType = item.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
                    
                    // Filter for data agent types (adjust based on actual Fabric API response)
                    if (itemType?.Contains("DataAgent", StringComparison.OrdinalIgnoreCase) == true ||
                        itemType?.Contains("AIAgent", StringComparison.OrdinalIgnoreCase) == true ||
                        itemType?.Contains("Assistant", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var dataAgentInfo = new DataAgentInfo(
                            Id: item.GetProperty("id").GetString() ?? string.Empty,
                            Name: item.GetProperty("displayName").GetString() ?? string.Empty,
                            Description: item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                            WorkspaceId: workspaceId,
                            Type: itemType ?? "DataAgent",
                            State: item.TryGetProperty("state", out var state) ? state.GetString() ?? "Active" : "Active",
                            CreatedDate: item.TryGetProperty("createdDate", out var created) && created.TryGetDateTime(out var createdDateTime) ? createdDateTime : DateTime.MinValue,
                            ModifiedDate: item.TryGetProperty("modifiedDate", out var modified) && modified.TryGetDateTime(out var modifiedDateTime) ? modifiedDateTime : DateTime.MinValue,
                            Tags: item.TryGetProperty("tags", out var tags) ? tags.EnumerateArray().Select(t => t.GetString()).Where(t => !string.IsNullOrEmpty(t)).Cast<string>().ToArray() : null
                        );
                        dataAgents.Add(dataAgentInfo);
                    }
                }
            }

            _logger.LogInformation("Successfully retrieved {DataAgentCount} data agents from workspace {WorkspaceId}", dataAgents.Count, workspaceId);
            return dataAgents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list data agents in workspace '{WorkspaceId}': {ErrorMessage}", workspaceId, ex.Message);
            throw;
        }
    }
}
