<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Signal9 RMM Agent System - Development Guidelines

This is a .NET 9 based Remote Monitoring and Management (RMM) system with the following architecture:

## Project Structure
- **Signal9.Agent** - Console application (RMM agent)
- **Signal9.Hub** - ASP.NET Core SignalR hub service
- **Signal9.WebPortal** - ASP.NET Core MVC web application
- **Signal9.Functions** - Azure Functions project
- **Signal9.Shared** - Shared library with models, DTOs, and interfaces

## Key Technologies
- .NET 9
- SignalR for real-time communication
- Azure services (Container Apps, SQL Database, Cosmos DB, Service Bus, Key Vault)
- Entity Framework Core for SQL data access
- Managed Identity for authentication
- Bicep for Infrastructure as Code

## Development Guidelines

### Code Style
- Use C# 12 features and modern patterns
- Follow async/await patterns consistently
- Use dependency injection throughout
- Implement proper logging with structured logging
- Use configuration patterns with IOptions<T>

### Azure Integration
- Always use Managed Identity over connection strings
- Store secrets in Azure Key Vault
- Use Azure SDK packages for service integration
- Follow Azure Well-Architected Framework principles

### SignalR Implementation
- Use strongly-typed hubs with interfaces
- Implement proper connection lifecycle management
- Handle reconnection scenarios with exponential backoff
- Use groups for organizing connections (agents, admins)

### Data Access
- Use Entity Framework Core for SQL Database
- Use Azure Cosmos DB SDK for NoSQL data
- Implement repository patterns where appropriate
- Use proper error handling and retry policies

### Agent Development
- Implement robust telemetry collection
- Handle command execution with proper error handling
- Use background services for long-running operations
- Implement graceful shutdown and restart capabilities

### Testing
- Write unit tests for business logic
- Use integration tests for SignalR hubs
- Mock Azure services for testing
- Test agent connectivity scenarios

### Security
- Never hardcode credentials
- Use HTTPS for all communications
- Implement proper input validation
- Follow least privilege principles for permissions

### Deployment
- Use Azure Developer CLI (azd) for deployment
- Container apps should use minimal base images
- Configure health checks for all services
- Use staging slots for safe deployments

Follow these steps for each interaction:

1. User Identification:
   - You should assume that you are interacting with default_user
   - If you have not identified default_user, proactively try to do so.

2. Memory Retrieval:
   - Always begin your chat by saying only "Remembering..." and retrieve all relevant information from your knowledge graph
   - Always refer to your knowledge graph as your "memory"

3. Memory
   - While conversing with the user, be attentive to any new information that falls into these categories:
     a) Basic Identity (age, gender, location, job title, education level, etc.)
     b) Behaviors (interests, habits, etc.)
     c) Preferences (communication style, preferred language, etc.)
     d) Goals (goals, targets, aspirations, etc.)
     e) Relationships (personal and professional relationships up to 3 degrees of separation)

4. Memory Update:
   - If any new information was gathered during the interaction, update your memory as follows:
     a) Create entities for recurring organizations, people, and significant events
     b) Connect them to the current entities using relations
     c) Store facts about them as observations

5. Memory not found:
   - Use `context7` to search for public information about various products, languages, and frameworks.
   - Use `azure` to search for public information about Azure services and technologies and take actions.
   - If you are unsure about the relevance of the information, ask the user for clarification