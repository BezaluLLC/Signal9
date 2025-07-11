using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Signal9.Shared.Configuration;
using Signal9.Shared.DTOs;
using Signal9.Shared.Interfaces;
using System.Management;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Signal9.Agent.Services;

/// <summary>
/// Main agent service that handles communication with the SignalR hub
/// </summary>
public class AgentService : BackgroundService
{
    private readonly ILogger<AgentService> _logger;
    private readonly AgentConfiguration _config;
    private readonly ITelemetryCollector _telemetryCollector;
    private readonly ISystemInfoProvider _systemInfoProvider;
    private HubConnection? _hubConnection;
    private Timer? _heartbeatTimer;
    private Timer? _telemetryTimer;
    private string _agentId;
    private int _reconnectAttempts = 0;

    public AgentService(
        ILogger<AgentService> logger,
        IOptions<AgentConfiguration> config,
        ITelemetryCollector telemetryCollector,
        ISystemInfoProvider systemInfoProvider)
    {
        _logger = logger;
        _config = config.Value;
        _telemetryCollector = telemetryCollector;
        _systemInfoProvider = systemInfoProvider;
        _agentId = Environment.MachineName + "_" + Guid.NewGuid().ToString("N")[..8];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent service starting with ID: {AgentId}", _agentId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectToHub();
                await RegisterAgent();
                StartTimers();

                // Keep the connection alive
                while (_hubConnection?.State == HubConnectionState.Connected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Connection lost, attempting to reconnect...");
                    await HandleReconnection();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent service execution");
                await Task.Delay(TimeSpan.FromSeconds(_config.ReconnectDelay), stoppingToken);
            }
        }
    }

    private async Task ConnectToHub()
    {
        if (string.IsNullOrEmpty(_config.HubUrl))
        {
            throw new InvalidOperationException("Hub URL not configured");
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_config.HubUrl)
            .WithAutomaticReconnect(new[] { 
                TimeSpan.FromSeconds(0), 
                TimeSpan.FromSeconds(2), 
                TimeSpan.FromSeconds(10), 
                TimeSpan.FromSeconds(30) 
            })
            .Build();

        // Set up event handlers
        _hubConnection.On<CommandDto>("ExecuteCommand", ExecuteCommandAsync);
        _hubConnection.On<object>("UpdateConfiguration", UpdateConfigurationAsync);
        _hubConnection.On<string[]>("CollectTelemetry", CollectTelemetryAsync);
        _hubConnection.On("RestartAgent", RestartAgentAsync);
        _hubConnection.On("ShutdownAgent", ShutdownAgentAsync);
        _hubConnection.On<string>("ConnectionStatusChanged", OnConnectionStatusChanged);

        _hubConnection.Reconnecting += async (exception) =>
        {
            _logger.LogWarning("Connection lost, reconnecting... Exception: {Exception}", exception?.Message);
            StopTimers();
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _logger.LogInformation("Reconnected successfully with connection ID: {ConnectionId}", connectionId);
            _reconnectAttempts = 0;
            await RegisterAgent();
            StartTimers();
        };

        _hubConnection.Closed += async (exception) =>
        {
            _logger.LogError("Connection closed. Exception: {Exception}", exception?.Message);
            StopTimers();
        };

        await _hubConnection.StartAsync();
        _logger.LogInformation("Connected to SignalR hub");
    }

    private async Task RegisterAgent()
    {
        try
        {
            var systemInfo = await _systemInfoProvider.GetSystemInfoAsync();
            var registrationData = new AgentRegistrationDto
            {
                AgentId = _agentId,
                TenantCode = _config.TenantCode ?? "default",
                MachineName = systemInfo.MachineName,
                OperatingSystem = systemInfo.OperatingSystem,
                Architecture = systemInfo.Architecture,
                Version = systemInfo.Version ?? "1.0.0",
                Tags = new Dictionary<string, string>
                {
                    { "Domain", systemInfo.Domain ?? "Unknown" },
                    { "OSVersion", systemInfo.OSVersion ?? "Unknown" },
                    { "TotalMemoryMB", systemInfo.TotalMemoryMB.ToString() ?? "0" },
                    { "ProcessorCores", systemInfo.ProcessorCores.ToString() ?? "0" },
                    { "ProcessorName", systemInfo.ProcessorName ?? "Unknown" },
                    { "IpAddress", systemInfo.IpAddress ?? "Unknown" },
                    { "MacAddress", systemInfo.MacAddress ?? "Unknown" },
                    { "GroupName", _config.GroupName ?? "default" }
                }
            };

            await _hubConnection!.InvokeAsync("RegisterAgent", _agentId, registrationData);
            _logger.LogInformation("Agent registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register agent");
        }
    }

    private void StartTimers()
    {
        StopTimers();

        _heartbeatTimer = new Timer(SendHeartbeat, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(_config.HeartbeatInterval));

        _telemetryTimer = new Timer(SendTelemetry, null, 
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(_config.TelemetryInterval));
    }

    private void StopTimers()
    {
        _heartbeatTimer?.Dispose();
        _telemetryTimer?.Dispose();
    }

    private async void SendHeartbeat(object? state)
    {
        try
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("Heartbeat", _agentId);
                _logger.LogDebug("Heartbeat sent");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat");
        }
    }

    private async void SendTelemetry(object? state)
    {
        try
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                var telemetryData = await _telemetryCollector.CollectTelemetryAsync();
                await _hubConnection.InvokeAsync("SendTelemetry", _agentId, telemetryData);
                _logger.LogDebug("Telemetry sent");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending telemetry");
        }
    }

    private async Task ExecuteCommandAsync(CommandDto command)
    {
        _logger.LogInformation("Executing command: {CommandType} with ID: {CommandId}", 
            command.CommandType, command.CommandId);

        var result = new CommandResultDto
        {
            CommandId = command.CommandId,
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            // TODO: Implement command execution logic
            switch (command.CommandType.ToLower())
            {
                case "ping":
                    result.Result = "pong";
                    result.Status = "Completed";
                    break;
                case "systeminfo":
                    var systemInfo = await _systemInfoProvider.GetSystemInfoAsync();
                    result.Result = System.Text.Json.JsonSerializer.Serialize(systemInfo);
                    result.Status = "Completed";
                    break;
                default:
                    result.ErrorMessage = $"Unknown command type: {command.CommandType}";
                    result.Status = "Failed";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            result.Status = "Failed";
            _logger.LogError(ex, "Error executing command {CommandId}", command.CommandId);
        }

        result.CompletedAt = DateTime.UtcNow;

        try
        {
            await _hubConnection!.InvokeAsync("ReportCommandResult", command.CommandId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting command result for {CommandId}", command.CommandId);
        }
    }

    private async Task UpdateConfigurationAsync(object configuration)
    {
        _logger.LogInformation("Received configuration update");
        // TODO: Implement configuration update logic
        await Task.CompletedTask;
    }

    private async Task CollectTelemetryAsync(string[] metrics)
    {
        _logger.LogInformation("Received telemetry collection request for metrics: {Metrics}", 
            string.Join(", ", metrics));
        // TODO: Implement on-demand telemetry collection
        await Task.CompletedTask;
    }

    private async Task RestartAgentAsync()
    {
        _logger.LogInformation("Received restart command");
        // TODO: Implement agent restart logic
        await Task.CompletedTask;
    }

    private async Task ShutdownAgentAsync()
    {
        _logger.LogInformation("Received shutdown command");
        // TODO: Implement graceful shutdown logic
        await Task.CompletedTask;
    }

    private void OnConnectionStatusChanged(string status)
    {
        _logger.LogInformation("Connection status changed to: {Status}", status);
    }

    private async Task HandleReconnection()
    {
        _reconnectAttempts++;
        if (_reconnectAttempts > _config.MaxReconnectAttempts)
        {
            _logger.LogError("Max reconnection attempts reached. Shutting down.");
            return;
        }

        var delay = TimeSpan.FromSeconds(_config.ReconnectDelay * _reconnectAttempts);
        _logger.LogInformation("Reconnection attempt {Attempt} in {Delay} seconds", 
            _reconnectAttempts, delay.TotalSeconds);
        
        await Task.Delay(delay);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent service stopping");
        
        StopTimers();
        
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
