using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Signal9.Shared.Configuration;
using Signal9.Shared.DTOs;
using Signal9.Shared.Models;
using System.Text.Json;
using System.Text;

namespace Signal9.Agent.Services;    /// <summary>
    /// Main agent service that handles communication with the Agent Functions and SignalR service
    /// </summary>
public class AgentService : BackgroundService
{
    private readonly ILogger<AgentService> _logger;
    private readonly AgentConfiguration _config;
    private readonly ITelemetryCollector _telemetryCollector;
    private readonly ISystemInfoProvider _systemInfoProvider;
    private readonly HttpClient _httpClient;
    private HubConnection? _signalRConnection;
    private Timer? _heartbeatTimer;
    private Timer? _telemetryTimer;
    private string _agentId;
    private int _reconnectAttempts = 0;
    private readonly JsonSerializerOptions _jsonOptions;

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
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent service starting with ID: {AgentId}", _agentId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Register agent with Agent Functions
                await RegisterWithAgentFunctions();
                
                // Connect to SignalR for real-time communication
                await ConnectToSignalR();

                // Start periodic tasks
                StartTimers();

                // Keep the service running while connected
                while (_signalRConnection?.State == HubConnectionState.Connected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent service execution");
                _reconnectAttempts++;
                
                // Exponential backoff for reconnection attempts
                var delaySeconds = Math.Min(Math.Pow(2, _reconnectAttempts), 300); // Max 5 minutes
                var delay = TimeSpan.FromSeconds(delaySeconds);
                _logger.LogInformation("Reconnecting in {Delay} seconds (attempt {Attempt})", delay.TotalSeconds, _reconnectAttempts);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task RegisterWithAgentFunctions()
    {
        try
        {
            var systemInfo = await _systemInfoProvider.GetSystemInfoAsync();
            var registrationData = new AgentRegistrationDto
            {
                AgentId = _agentId,
                TenantCode = _config.TenantCode ?? string.Empty,
                MachineName = Environment.MachineName,
                OperatingSystem = systemInfo.OperatingSystem,
                Architecture = systemInfo.Architecture ?? string.Empty,
                LastSeen = DateTime.UtcNow,
                Version = "1.0.0",
                IsOnline = true
            };

            var json = JsonSerializer.Serialize(registrationData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var registrationUrl = $"{_config.AgentFunctionsUrl}/api/RegisterAgent";
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _config.FunctionKey);

            var response = await _httpClient.PostAsync(registrationUrl, content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Agent {AgentId} registered successfully with Agent Functions", _agentId);
                _reconnectAttempts = 0; // Reset reconnect attempts on successful registration
            }
            else
            {
                _logger.LogError("Failed to register agent {AgentId} with Agent Functions. Status: {StatusCode}", 
                    _agentId, response.StatusCode);
                throw new InvalidOperationException($"Registration failed with status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent {AgentId} with Agent Functions", _agentId);
            throw;
        }
    }

    private async Task ConnectToSignalR()
    {
        try
        {        _signalRConnection = new HubConnectionBuilder()
            .WithUrl($"{_config.AgentFunctionsUrl}/api")
            .WithAutomaticReconnect()
            .Build();

            // Set up event handlers
            _signalRConnection.On<AgentCommand>("ExecuteCommand", ExecuteCommandAsync);
            _signalRConnection.On<object>("UpdateConfiguration", UpdateConfigurationAsync);
            _signalRConnection.On<string[]>("CollectTelemetry", CollectTelemetryAsync);
            _signalRConnection.On("RestartAgent", RestartAgentAsync);
            _signalRConnection.On("ShutdownAgent", ShutdownAgentAsync);
            _signalRConnection.On<string>("ConnectionStatusChanged", OnConnectionStatusChanged);

            _signalRConnection.Reconnecting += (exception) =>
            {
                _logger.LogWarning("SignalR connection lost. Reconnecting... Exception: {Exception}", exception?.Message);
                return Task.CompletedTask;
            };

            _signalRConnection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                _reconnectAttempts = 0;
                return Task.CompletedTask;
            };

            _signalRConnection.Closed += (exception) =>
            {
                _logger.LogError("SignalR connection closed. Exception: {Exception}", exception?.Message);
                return Task.CompletedTask;
            };

            await _signalRConnection.StartAsync();
            _logger.LogInformation("Connected to SignalR service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR service");
            throw;
        }
    }

    private void StartTimers()
    {
        // Start heartbeat timer
        _heartbeatTimer = new Timer(async _ => await SendHeartbeatAsync(), 
            null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        // Start telemetry timer
        _telemetryTimer = new Timer(async _ => await SendTelemetryAsync(), 
            null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            if (_signalRConnection?.State == HubConnectionState.Connected)
            {
                // Send heartbeat via HTTP to Agent Functions
                var heartbeatData = new { AgentId = _agentId, Timestamp = DateTime.UtcNow };
                var json = JsonSerializer.Serialize(heartbeatData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var heartbeatUrl = $"{_config.AgentFunctionsUrl}/api/agents/{_agentId}/heartbeat";
                await _httpClient.PostAsync(heartbeatUrl, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat for agent {AgentId}", _agentId);
        }
    }

    private async Task SendTelemetryAsync()
    {
        try
        {
            var telemetryData = await _telemetryCollector.CollectTelemetryAsync();
            telemetryData.AgentId = _agentId;
            telemetryData.TenantCode = _config.TenantCode ?? string.Empty;

            var json = JsonSerializer.Serialize(telemetryData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var telemetryUrl = $"{_config.AgentFunctionsUrl}/api/ReceiveTelemetry";
            var response = await _httpClient.PostAsync(telemetryUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Telemetry sent successfully for agent {AgentId}", _agentId);
            }
            else
            {
                _logger.LogWarning("Failed to send telemetry for agent {AgentId}. Status: {StatusCode}", 
                    _agentId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending telemetry for agent {AgentId}", _agentId);
        }
    }

    private async Task ExecuteCommandAsync(AgentCommand command)
    {
        _logger.LogInformation("Executing command {CommandType} for agent {AgentId}", command.CommandType, _agentId);

        try
        {
            // TODO: Implement command execution logic
            object result;
            switch (command.CommandType)
            {
                case "GetSystemInfo":
                    result = await _systemInfoProvider.GetSystemInfoAsync();
                    break;
                case "RestartService":
                    {
                        // For now, use a hardcoded service name until Parameters issue is resolved
                        var serviceName = "default-service";
                        await Task.Delay(100); // Placeholder for service restart logic
                        result = new { Success = true, Message = $"Service {serviceName} restart initiated" };
                    }
                    break;
                case "RunScript":
                    {
                        // For now, use a hardcoded script until Parameters issue is resolved
                        await Task.Delay(100); // Placeholder for script execution logic
                        result = new { Success = true, Output = "Script executed successfully" };
                    }
                    break;
                default:
                    result = new { Error = $"Unknown command type: {command.CommandType}" };
                    break;
            }

            _logger.LogInformation("Command {CommandType} executed successfully", command.CommandType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command {CommandType}", command.CommandType);
        }
    }

    // Method definitions removed due to compilation conflicts
    
    private async Task UpdateConfigurationAsync(object configuration)
    {
        _logger.LogInformation("Updating configuration for agent {AgentId}", _agentId);
        // TODO: Implement configuration update logic
        await Task.CompletedTask;
    }

    private async Task CollectTelemetryAsync(string[] metrics)
    {
        _logger.LogInformation("Collecting specific telemetry metrics for agent {AgentId}: {Metrics}", 
            _agentId, string.Join(", ", metrics));
        await SendTelemetryAsync();
    }

    private async Task RestartAgentAsync()
    {
        _logger.LogInformation("Restart requested for agent {AgentId}", _agentId);
        // TODO: Implement agent restart logic
        await Task.CompletedTask;
    }

    private async Task ShutdownAgentAsync()
    {
        _logger.LogInformation("Shutdown requested for agent {AgentId}", _agentId);
        // TODO: Implement agent shutdown logic
        await Task.CompletedTask;
    }

    private async Task OnConnectionStatusChanged(string status)
    {
        _logger.LogInformation("Connection status changed to {Status} for agent {AgentId}", status, _agentId);
        await Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent service stopping for agent {AgentId}", _agentId);

        _heartbeatTimer?.Dispose();
        _telemetryTimer?.Dispose();

        if (_signalRConnection != null)
        {
            await _signalRConnection.DisposeAsync();
        }

        _httpClient?.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
