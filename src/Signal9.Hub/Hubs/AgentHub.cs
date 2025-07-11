using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Signal9.Shared.DTOs;
using Signal9.Shared.Interfaces;

namespace Signal9.Hub.Hubs;

/// <summary>
/// SignalR hub for agent communication
/// </summary>
public class AgentHub : Microsoft.AspNetCore.SignalR.Hub, IAgentHub
{
    private readonly ILogger<AgentHub> _logger;
    private const string AGENTS_GROUP = "Agents";

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Agent connected: {ConnectionId}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, AGENTS_GROUP);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Agent disconnected: {ConnectionId}, Exception: {Exception}", 
            Context.ConnectionId, exception?.Message);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AGENTS_GROUP);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterAgent(string agentId, object registrationData)
    {
        try
        {
            _logger.LogInformation("Agent registration: {AgentId}, Connection: {ConnectionId}", 
                agentId, Context.ConnectionId);
            
            // Store agent connection mapping
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Agent_{agentId}");
            
            // TODO: Process registration data and store in database
            // TODO: Send initial configuration to agent
            
            await Clients.Caller.SendAsync("ConnectionStatusChanged", "registered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent {AgentId}", agentId);
            await Clients.Caller.SendAsync("ConnectionStatusChanged", "error");
        }
    }

    public async Task SendTelemetry(string agentId, object telemetryData)
    {
        try
        {
            _logger.LogDebug("Received telemetry from agent {AgentId}", agentId);
            
            // TODO: Process and store telemetry data
            // TODO: Send to Azure Service Bus for processing
            // TODO: Store in Cosmos DB
            
            // Broadcast to connected admin clients
            await Clients.Group("Admins").SendAsync("TelemetryReceived", agentId, telemetryData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing telemetry from agent {AgentId}", agentId);
        }
    }

    public async Task UpdateStatus(string agentId, string status)
    {
        try
        {
            _logger.LogInformation("Agent {AgentId} status updated to {Status}", agentId, status);
            
            // TODO: Update agent status in database
            
            // Broadcast to connected admin clients
            await Clients.Group("Admins").SendAsync("AgentStatusChanged", agentId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for agent {AgentId}", agentId);
        }
    }

    public async Task Heartbeat(string agentId)
    {
        try
        {
            _logger.LogDebug("Heartbeat received from agent {AgentId}", agentId);
            
            // TODO: Update last seen timestamp in database
            
            // Update connection timestamp
            Context.Items["LastHeartbeat"] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing heartbeat from agent {AgentId}", agentId);
        }
    }

    public async Task ReportCommandResult(string commandId, object result)
    {
        try
        {
            _logger.LogInformation("Command result received for command {CommandId}", commandId);
            
            // TODO: Update command status in database
            // TODO: Send result to Service Bus for processing
            
            // Notify admin clients
            await Clients.Group("Admins").SendAsync("CommandResultReceived", commandId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command result for command {CommandId}", commandId);
        }
    }

    public async Task SendLogs(string agentId, object[] logEntries)
    {
        try
        {
            _logger.LogDebug("Received {Count} log entries from agent {AgentId}", 
                logEntries.Length, agentId);
            
            // TODO: Store logs in Cosmos DB
            // TODO: Send to Application Insights
            
            // Broadcast to connected admin clients
            await Clients.Group("Admins").SendAsync("LogsReceived", agentId, logEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing logs from agent {AgentId}", agentId);
        }
    }

    // Admin methods
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin client connected: {ConnectionId}", Context.ConnectionId);
    }

    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin client disconnected: {ConnectionId}", Context.ConnectionId);
    }

    public async Task SendCommandToAgent(string agentId, CommandDto command)
    {
        try
        {
            _logger.LogInformation("Sending command {CommandId} to agent {AgentId}", 
                command.CommandId, agentId);
            
            // TODO: Store command in database
            
            // Send command to specific agent
            await Clients.Group($"Agent_{agentId}").SendAsync("ExecuteCommand", command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command {CommandId} to agent {AgentId}", 
                command.CommandId, agentId);
        }
    }
}
