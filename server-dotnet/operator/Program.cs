using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using RoomOperator.Abstractions;
using RoomOperator.Clients;
using RoomOperator.Core;
using RoomOperator.HttpApi;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Read configuration
var config = builder.Configuration;
var operatorVersion = config["Operator:Version"] ?? "1.0.0";
var baseUrl = config["RoomServer:BaseUrl"] ?? "http://localhost:5000";
var authToken = Environment.GetEnvironmentVariable("ROOM_AUTH_TOKEN") ?? config["Auth:Token"];
var mcpEnabled = config.GetValue<bool>("Operator:Features:Resources");

// Helper method to configure HTTP client with authentication
void ConfigureAuthenticatedClient(HttpClient client)
{
    client.BaseAddress = new Uri(baseUrl);
    if (!string.IsNullOrEmpty(authToken) && authToken != Constants.AuthTokenPlaceholder)
    {
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
    }
}

// Configure HTTP clients
builder.Services.AddHttpClient<IRoomClient, SignalRClient>(ConfigureAuthenticatedClient);
builder.Services.AddHttpClient<IArtifactsClient, ArtifactsClient>(ConfigureAuthenticatedClient);
builder.Services.AddHttpClient<IPoliciesClient, PoliciesClient>(ConfigureAuthenticatedClient);
builder.Services.AddHttpClient<IMcpClient, McpClient>(client => {
    ConfigureAuthenticatedClient(client);
}).AddTypedClient((httpClient, sp) => {
    var logger = sp.GetRequiredService<ILogger<McpClient>>();
    return new McpClient(httpClient, logger, mcpEnabled);
});

// Configure core services
builder.Services.AddSingleton(sp => 
{
    var retryConfig = new RetryPolicyConfig
    {
        MaxAttempts = config.GetValue<int>("Operator:Retry:MaxAttempts", 3),
        InitialDelayMs = config.GetValue<int>("Operator:Retry:InitialDelayMs", 100),
        MaxDelayMs = config.GetValue<int>("Operator:Retry:MaxDelayMs", 5000),
        JitterFactor = config.GetValue<double>("Operator:Retry:JitterFactor", 0.2)
    };
    var logger = sp.GetRequiredService<ILogger<RetryPolicyFactory>>();
    return new RetryPolicyFactory(retryConfig, logger);
});

builder.Services.AddSingleton(sp =>
{
    var guardrailsConfig = new GuardrailsConfig
    {
        MaxEntitiesKickPerCycle = config.GetValue<int>("Operator:Reconciliation:Guardrails:MaxEntitiesKickPerCycle", 5),
        MaxArtifactsDeletePerCycle = config.GetValue<int>("Operator:Reconciliation:Guardrails:MaxArtifactsDeletePerCycle", 10),
        ChangeThreshold = config.GetValue<double>("Operator:Reconciliation:Guardrails:ChangeThreshold", 0.5),
        RequireConfirmHeader = config.GetValue<bool>("Operator:Reconciliation:Guardrails:RequireConfirmHeader", true)
    };
    var logger = sp.GetRequiredService<ILogger<Guardrails>>();
    return new Guardrails(guardrailsConfig, logger);
});

builder.Services.AddSingleton<DiffEngine>();
builder.Services.AddSingleton<AuditLog>();

builder.Services.AddSingleton(sp =>
{
    var roomClient = sp.GetRequiredService<IRoomClient>();
    var artifactsClient = sp.GetRequiredService<IArtifactsClient>();
    var policiesClient = sp.GetRequiredService<IPoliciesClient>();
    var mcpClient = sp.GetRequiredService<IMcpClient>();
    var diffEngine = sp.GetRequiredService<DiffEngine>();
    var guardrails = sp.GetRequiredService<Guardrails>();
    var retryPolicy = sp.GetRequiredService<RetryPolicyFactory>();
    var auditLog = sp.GetRequiredService<AuditLog>();
    var logger = sp.GetRequiredService<ILogger<ReconcilePhases>>();
    
    return new ReconcilePhases(
        roomClient, artifactsClient, policiesClient, mcpClient,
        diffEngine, guardrails, retryPolicy, auditLog, logger, operatorVersion);
});

builder.Services.AddSingleton<RoomOperatorService>();

// Add controllers for JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Configure middleware
app.UseRouting();

// Map Prometheus metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();

// Map operator endpoints
app.MapOperatorEndpoints();
app.MapEventsEndpoints();

// Start the application
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("RoomOperator v{Version} starting...", operatorVersion);
logger.LogInformation("Connecting to RoomServer at {BaseUrl}", baseUrl);
logger.LogInformation("MCP features enabled: {McpEnabled}", mcpEnabled);

var port = config.GetValue<int>("HttpApi:Port", 8080);
var operatorUrl = config["HttpApi:OperatorUrl"] ?? "http://localhost";

app.Run($"{operatorUrl}:{port}");
