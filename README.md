# Signal9 RMM Agent System

A comprehensive Remote Monitoring and Management (RMM) system built on .NET 9 with SignalR for real-time communication between agents and the central hub.

## Architecture Overview

Signal9 is designed as a modern, cloud-native RMM solution with the following components:

### Core Components

- **Signal9.Agent** - Console application that runs on client machines to collect telemetry and execute commands
- **Signal9.Hub** - SignalR hub service for real-time communication with agents
- **Signal9.WebPortal** - Web-based management interface for monitoring and controlling agents
- **Signal9.Functions** - Azure Functions for backend data processing and API operations
- **Signal9.Shared** - Common models, interfaces, and utilities shared across components

### Azure Services

- **Azure Container Apps** - Hosting for Hub and Web Portal
- **Azure Functions** - Serverless backend processing
- **Azure SignalR Service** - Managed SignalR for scalable real-time communication
- **Azure SQL Database** - Relational data (agents, users, configurations)
- **Azure Cosmos DB** - Non-relational data (telemetry, logs, events)
- **Azure Service Bus** - Message queuing for commands and events
- **Azure Key Vault** - Secrets and configuration management
- **Azure Application Insights** - Monitoring and diagnostics
- **Azure Container Registry** - Container image storage

## Features

### Agent Capabilities
- Real-time system monitoring (CPU, memory, disk, network)
- Remote command execution
- Event log collection
- System information reporting
- Automatic reconnection with exponential backoff
- Configurable telemetry collection intervals

### Hub & Management
- Real-time agent status monitoring
- Command dispatching to agents
- Telemetry data aggregation
- Multi-tenant support
- Role-based access control
- Scalable architecture with Azure SignalR

### Security
- Managed Identity authentication
- Encrypted communication
- Secure secret storage in Key Vault
- Network-level security with Virtual Networks

## Getting Started

### Prerequisites

- .NET 9 SDK
- Azure CLI
- Azure Developer CLI (azd)
- Docker (for containerization)
- Visual Studio 2022 or VS Code

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Signal9
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the Hub locally**
   ```bash
   cd src/Signal9.Hub
   dotnet run
   ```

5. **Run the Agent locally**
   ```bash
   cd src/Signal9.Agent
   dotnet run
   ```

### Azure Deployment

1. **Initialize Azure Developer CLI**
   ```bash
   azd init
   ```

2. **Deploy to Azure**
   ```bash
   azd up
   ```

3. **Configure SQL password when prompted**

## Configuration

### Agent Configuration (appsettings.json)

```json
{
  "AgentConfiguration": {
    "HubUrl": "https://your-hub-url/agentHub",
    "TenantCode": "your-tenant",
    "GroupName": "default",
    "HeartbeatInterval": 30,
    "TelemetryInterval": 60,
    "ReconnectDelay": 5,
    "MaxReconnectAttempts": 10
  }
}
```

### Hub Configuration

The hub automatically configures itself using Azure services when deployed to Azure:
- SignalR connection from Azure SignalR Service
- Key Vault for secrets
- Application Insights for monitoring

## Project Structure

```
Signal9/
├── src/
│   ├── Signal9.Agent/          # RMM Agent console application
│   ├── Signal9.Hub/            # SignalR Hub web service
│   ├── Signal9.WebPortal/      # Management web application
│   ├── Signal9.Functions/      # Azure Functions
│   └── Signal9.Shared/         # Shared libraries
├── infra/                      # Bicep infrastructure templates
├── tests/                      # Unit and integration tests
├── docs/                       # Documentation
├── azure.yaml                  # Azure Developer CLI configuration
└── Signal9.sln                 # Solution file
```

## Development

### Adding New Telemetry Metrics

1. Update `TelemetryDto` in `Signal9.Shared/DTOs/AgentDTOs.cs`
2. Implement collection logic in `TelemetryCollector.cs`
3. Update the hub to handle new metrics

### Adding New Agent Commands

1. Define command type in `AgentCommand.cs`
2. Implement execution logic in `AgentService.ExecuteCommandAsync`
3. Add command dispatching in the hub or web portal

### Database Migrations

Entity Framework migrations are handled automatically during deployment.

## Monitoring

- **Application Insights** - Performance monitoring and diagnostics
- **Azure Monitor** - Infrastructure monitoring
- **Log Analytics** - Centralized logging
- **Azure Alerts** - Proactive notifications

## Security Considerations

- All inter-service communication uses Managed Identity
- Secrets are stored in Azure Key Vault
- Network traffic is encrypted with TLS
- SQL connections use Azure AD authentication
- RBAC controls access to Azure resources

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
- Create an issue in the repository
- Review the documentation in the `/docs` folder
- Check Azure Monitor logs for runtime issues
