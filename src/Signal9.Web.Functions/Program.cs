using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Azure.Identity;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add Azure Key Vault configuration for production
        if (!context.HostingEnvironment.IsDevelopment())
        {
            var keyVaultUrl = config.Build()["AzureConfiguration:KeyVaultUrl"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                config.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
            }
        }
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Add HTTP client
        services.AddHttpClient();
    })
    .Build();

host.Run();
