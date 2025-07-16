# Data Agent Discovery and Query Commands

This document explains how to use the new Azure MCP Data Agent discovery and query commands that enable LLM-driven data agent selection and interaction.

## Overview

The Data Agent commands provide a complete workflow for discovering and querying Azure Data Agents in Microsoft Fabric workspaces. These commands are designed to work seamlessly with LLMs for intelligent data agent selection and interaction.

## Available Commands

### 1. `azmcp dataagent list-workspaces`

Lists all available workspaces that contain data agents.

**Usage:**

```bash
azmcp dataagent list-workspaces
```

**Output:**

- Returns a list of all workspaces with their IDs, names, descriptions, capacity IDs, and states
- Includes a summary formatted for LLM consumption
- Shows only active workspaces available for data agent operations

### 2. `azmcp dataagent list --workspace-id <workspace-id>`

Lists all data agents within a specific workspace.

**Usage:**

```bash
azmcp dataagent list --workspace-id "12345678-1234-1234-1234-123456789abc"
```

**Output:**

- Returns all data agents in the specified workspace
- Includes agent metadata: ID, name, description, type, state, tags
- Provides selection guidance for LLM to choose appropriate agents
- Shows only active agents ready for queries

### 3. `azmcp dataagent discover --query "<search-query>" [--workspace-id <workspace-id>]`

Intelligently discovers data agents across workspaces based on a query.

**Usage:**

```bash
# Search across all workspaces
azmcp dataagent discover --query "sales data analysis"

# Search within a specific workspace
azmcp dataagent discover --query "financial reporting" --workspace-id "12345678-1234-1234-1234-123456789abc"
```

**Output:**

- Comprehensive discovery summary with all available data agents
- Suggested data agents based on keyword matching
- Ready-to-use command examples for querying discovered agents
- Workspace information and capacity details

### 4. `azmcp dataagent query` (Enhanced)

Query a specific data agent with optional streaming support.

**Usage:**

```bash
# Basic query
azmcp dataagent query --workspace-id "12345678-1234-1234-1234-123456789abc" --data-agent-id "agent-123" --capacity-id "capacity-456" --query "What are the latest sales figures?"

# Query with streaming
azmcp dataagent query --workspace-id "12345678-1234-1234-1234-123456789abc" --data-agent-id "agent-123" --capacity-id "capacity-456" --query "Analyze quarterly performance" --enable-streaming
```

## LLM Discovery Workflow

The recommended workflow for LLM-driven data agent discovery and querying:

### Step 1: Discover Available Data Agents

```bash
azmcp dataagent discover --query "your question or topic"
```

This command will:

- Search across all workspaces for relevant data agents
- Provide suggestions based on agent names, descriptions, and tags
- Return executable command examples

### Step 2: Execute Query

Use the provided command examples from the discovery step:

```bash
azmcp dataagent query --workspace-id <workspace-id> --data-agent-id <data-agent-id> --capacity-id <capacity-id> --query "your detailed question"
```

### Alternative: Scoped Discovery

If you know the workspace, you can use a more targeted approach:

```bash
# List workspaces first
azmcp dataagent list-workspaces

# List agents in a specific workspace
azmcp dataagent list --workspace-id "target-workspace-id"

# Query specific agent
azmcp dataagent query --workspace-id "target-workspace-id" --data-agent-id "chosen-agent-id" --capacity-id "workspace-capacity-id" --query "your question"
```

## Authentication

All commands use Azure authentication with the following scopes:

- `https://api.fabric.microsoft.com/.default` for workspace and data agent discovery
- `https://ml.azure.com/.default` for data agent querying

Ensure you have appropriate permissions:

- **Fabric.ReadWrite.All** for workspace discovery
- **Fabric.Item.ReadWrite.All** for data agent listing
- **ML.ReadWrite** for data agent querying

## Error Handling

The commands include comprehensive error handling:

- Invalid workspace IDs will return clear error messages
- Missing data agents will be reported with suggestions
- Authentication failures will provide guidance on required permissions
- Network timeouts include retry policies

## Streaming Support

The `query` command supports streaming responses with the `--enable-streaming` flag:

- Provides real-time progress updates
- Uses Server-Sent Events (SSE) for progressive results
- Ideal for long-running queries or complex analyses

## Example LLM Interaction

```text
Human: "I need to analyze sales data for Q3 2024"

LLM: I'll help you find and query the appropriate data agent for sales analysis. Let me discover available data agents.

Command: azmcp dataagent discover --query "sales data analysis Q3 2024"

Result: Found 3 data agents:
- Sales Analytics Agent (ID: agent-sales-001) in Marketing Workspace
- Financial Reporting Agent (ID: agent-finance-002) in Finance Workspace  
- Business Intelligence Agent (ID: agent-bi-003) in Analytics Workspace

Based on your request, I recommend the Sales Analytics Agent. Let me query it:

Command: azmcp dataagent query --workspace-id "marketing-workspace-123" --data-agent-id "agent-sales-001" --capacity-id "capacity-456" --query "Analyze sales data for Q3 2024 including trends, performance metrics, and growth opportunities"

[Agent provides detailed sales analysis...]
```

## Best Practices

1. **Use Discovery First**: Always start with `discover` command to find relevant agents
2. **Check Agent Status**: Only query agents that are in "Active" or "Ready" state
3. **Use Streaming**: Enable streaming for complex queries that may take time
4. **Scope Appropriately**: Use workspace-specific searches when possible for better performance
5. **Cache Results**: Workspace and agent lists can be cached for better performance

## API Integration

These commands use the Microsoft Fabric REST API endpoints:

- **Workspaces**: `https://api.fabric.microsoft.com/v1/workspaces`
- **Items**: `https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/items`
- **Data Agents**: Fabric-specific endpoints for AI/ML workloads

The implementation follows Azure best practices for:

- Managed Identity authentication
- Proper error handling and retries
- Comprehensive logging
- Security and performance optimization
