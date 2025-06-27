using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using LeaseLogic.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add Key Vault configuration in production
        var keyVaultUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            // Key Vault integration can be added later
            // config.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add Azure services
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddSingleton<IOpenAIService, OpenAIService>();
        services.AddSingleton<ILeaseAnalysisService, LeaseAnalysisService>();

        // Configure HTTP client
        services.AddHttpClient();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddApplicationInsights();
        });
    })
    .Build();

host.Run();