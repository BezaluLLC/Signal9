using Signal9.Hub.Hubs;
using Signal9.Shared.Configuration;
using Azure.Identity;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["AzureConfiguration:KeyVaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

// Add services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add Azure SignalR Service (when configured)
var signalRConnectionString = builder.Configuration.GetConnectionString("SignalR");
if (!string.IsNullOrEmpty(signalRConnectionString))
{
    builder.Services.AddSignalR().AddAzureSignalR(signalRConnectionString);
}

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["AzureConfiguration:ApplicationInsightsConnectionString"];
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add configuration
builder.Services.Configure<AzureConfiguration>(
    builder.Configuration.GetSection("AzureConfiguration"));

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();

// Map SignalR hub
app.MapHub<AgentHub>("/agentHub");

// Add health check endpoint
app.MapHealthChecks("/health");

// Add basic status endpoint
app.MapGet("/", () => "Signal9 Hub is running");

app.Run();
