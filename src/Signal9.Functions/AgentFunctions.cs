using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Signal9.Shared.Models;
using Signal9.Shared.DTOs;
using System.Net;
using System.Text.Json;

namespace Signal9.Functions
{
    public class AgentFunctions
    {
        private readonly ILogger<AgentFunctions> _logger;

        public AgentFunctions(ILogger<AgentFunctions> logger)
        {
            _logger = logger;
        }

        [Function("AgentRegistration")]
        public async Task<HttpResponseData> RegisterAgentAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing agent registration");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var registrationData = JsonSerializer.Deserialize<AgentRegistrationDto>(requestBody);

                if (registrationData == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid registration data");
                    return badRequestResponse;
                }

                // Process agent registration
                // - Validate tenant code
                // - Create agent record in database
                // - Generate authentication token
                // - Send welcome message

                _logger.LogInformation("Registered agent {AgentId} for tenant {TenantCode}", 
                    registrationData.AgentId, registrationData.TenantCode);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                    Success = true, 
                    Message = "Agent registered successfully" 
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing agent registration");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error processing agent registration");
                return errorResponse;
            }
        }

        [Function("AgentHealthCheck")]
        public async Task<HttpResponseData> HealthCheckAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing agent health check");

            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new 
                { 
                    Status = "Healthy", 
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0"
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing health check");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error processing health check");
                return errorResponse;
            }
        }

        [Function("AgentHeartbeat")]
        public async Task<HttpResponseData> HeartbeatAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing agent heartbeat");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var heartbeatData = JsonSerializer.Deserialize<AgentHeartbeatDto>(requestBody);

                if (heartbeatData == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid heartbeat data");
                    return badRequestResponse;
                }

                // Process heartbeat
                // - Update agent last seen timestamp
                // - Check for pending commands
                // - Update agent status

                _logger.LogInformation("Processed heartbeat for agent {AgentId}", heartbeatData.AgentId);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new 
                { 
                    Success = true, 
                    HasPendingCommands = false, // This would be determined by checking the database
                    ServerTime = DateTime.UtcNow
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error processing heartbeat");
                return errorResponse;
            }
        }
    }
}
