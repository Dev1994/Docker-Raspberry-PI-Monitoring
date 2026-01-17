using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// ================================
// ?? OPENTELEMETRY SETUP
// ================================
// OpenTelemetry provides observability through three pillars:
// 1. TRACES - Show request flows and timing
// 2. METRICS - Provide performance and usage data  
// 3. LOGS - Traditional text-based logging (not covered here)

// ?? Activity Source for custom tracing spans
// Use this to create custom spans for business logic
var activitySource = new ActivitySource("YourWebApi.Activities");

// ?? Custom Meter for route-specific metrics
// This allows us to track metrics per API endpoint
var customMeter = new Meter("YourWebApi.Metrics");

// ?? Define custom metrics we want to track
var requestCounter = customMeter.CreateCounter<long>(
    name: "http_requests_total", 
    description: "Total number of HTTP requests by route and status");

var requestDuration = customMeter.CreateHistogram<double>(
    name: "http_request_duration_seconds", 
    description: "Duration of HTTP requests by route");

// ?? Get OpenTelemetry endpoint (configured by Aspire)
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? 
                   Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? 
                   "http://localhost:4317";

Console.WriteLine($"?? OpenTelemetry OTLP Endpoint: {otlpEndpoint}");

// ? Configure OpenTelemetry with comprehensive observability
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "YourWebApi",
            serviceVersion: builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    
    // ?? TRACING: Captures request flows and timing
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()    // ? Automatic HTTP request tracing
        .AddHttpClientInstrumentation()   // ? Automatic outbound HTTP call tracing  
        .AddSource(activitySource.Name)   // ? Enable our custom spans
        .AddOtlpExporter(options =>       // ? Send traces to OpenTelemetry Collector
        {
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
            options.TimeoutMilliseconds = 10000;
            Console.WriteLine($"?? Tracing configured: {options.Endpoint}");
        }))
    
    // ?? METRICS: Captures performance and usage data  
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()   // ? Built-in HTTP request metrics
        .AddHttpClientInstrumentation()  // ? Outbound HTTP call metrics
        .AddRuntimeInstrumentation()     // ? .NET runtime metrics (GC, memory)
        .AddProcessInstrumentation()     // ? Process-level metrics (CPU, memory)
        .AddMeter(customMeter.Name)      // ? Our custom route-specific metrics
        .AddOtlpExporter(options =>      // ? Send metrics to OpenTelemetry Collector
        {
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
            options.TimeoutMilliseconds = 10000;
            Console.WriteLine($"?? Metrics configured: {options.Endpoint}");
        }));

// ================================
// ??? STANDARD ASP.NET CORE SETUP
// ================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

// Register OpenTelemetry components for dependency injection
builder.Services.AddSingleton(activitySource);
builder.Services.AddSingleton(customMeter);
builder.Services.AddSingleton(requestCounter);
builder.Services.AddSingleton(requestDuration);

var app = builder.Build();

// ================================
// ?? CUSTOM MIDDLEWARE FOR ROUTE TRACKING
// ================================
// This middleware captures detailed metrics for each API endpoint
// It records both request count and duration per route

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    var route = context.Request.Path.Value ?? "unknown";
    var method = context.Request.Method;
    
    using var activity = activitySource.StartActivity($"{method} {route}");
    activity?.SetTag("http.route", route);
    activity?.SetTag("http.method", method);
    
    try
    {
        await next();
        
        stopwatch.Stop();
        var statusCode = context.Response.StatusCode.ToString();
        
        requestCounter.Add(1, new TagList
        {
            { "route", route },
            { "method", method },
            { "status_code", statusCode }
        });
        
        requestDuration.Record(stopwatch.Elapsed.TotalSeconds, new TagList
        {
            { "route", route },
            { "method", method },
            { "status_code", statusCode }
        });
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.SetTag("http.status_code", statusCode);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        
        requestCounter.Add(1, new TagList
        {
            { "route", route },
            { "method", method },
            { "status_code", "500" }
        });
        
        requestDuration.Record(stopwatch.Elapsed.TotalSeconds, new TagList
        {
            { "route", route },
            { "method", method },
            { "status_code", "500" }
        });
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("http.status_code", "500");
        
        throw;
    }
});

// ================================
// ?? HTTP PIPELINE CONFIGURATION
// ================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// ================================
// ?? API ENDPOINTS
// ================================

// ?? Simple root endpoint
app.MapGet("/", () => "?? .NET API with OpenTelemetry - Ready for monitoring!");

// ?? Traffic generator - Creates diverse traffic for testing dashboards
app.MapGet("/generate-traffic", async (IHttpClientFactory httpClientFactory) =>
{
    Console.WriteLine("?? Starting traffic generation for dashboard testing...");
    
    using var activity = activitySource.StartActivity("TrafficGeneration");
    activity?.SetTag("test.purpose", "dashboard_testing");
    
    var tasks = new List<Task>();
    var random = new Random();
    
    for (int i = 0; i < 20; i++)
    {
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                var baseUrl = "http://localhost:5000"; // Adjust as needed
                
                var endpointChoice = random.Next(1, 101);
                string endpoint;
                
                if (endpointChoice <= 40)
                    endpoint = "/api/weather";
                else if (endpointChoice <= 70)
                    endpoint = "/api/test-telemetry";
                else if (endpointChoice <= 85)
                    endpoint = "/api/external-call";
                else if (endpointChoice <= 95)
                    endpoint = "/health";
                else
                    endpoint = "/api/error";
                
                await Task.Delay(random.Next(10, 500));
                
                var response = await httpClient.GetAsync($"{baseUrl}{endpoint}");
                Console.WriteLine($"?? Generated request to {endpoint}: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Traffic generation error: {ex.Message}");
            }
        }));
    }
    
    await Task.WhenAll(tasks);
    activity?.SetTag("requests.generated", tasks.Count);
    
    Console.WriteLine("? Traffic generation completed!");
    
    return new
    {
        Message = "?? Traffic generation completed!",
        RequestsGenerated = tasks.Count,
        Timestamp = DateTimeOffset.UtcNow,
        Note = "Check your Grafana dashboard to see the generated metrics!"
    };
});

// ??? Weather endpoint - Simulates typical business logic
app.MapGet("/api/weather", async () =>
{
    using var activity = activitySource.StartActivity("GetWeatherForecast");
    activity?.SetTag("operation.type", "business_logic");
    
    var delay = Random.Shared.Next(50, 200);
    await Task.Delay(delay);
    
    var forecasts = new[]
    {
        new { Date = DateTime.Now.AddDays(1), TemperatureC = Random.Shared.Next(-20, 55), Summary = "Sunny" },
        new { Date = DateTime.Now.AddDays(2), TemperatureC = Random.Shared.Next(-20, 55), Summary = "Cloudy" },
        new { Date = DateTime.Now.AddDays(3), TemperatureC = Random.Shared.Next(-20, 55), Summary = "Rainy" }
    };
    
    activity?.SetTag("forecast.days", forecasts.Length);
    activity?.SetTag("processing.duration_ms", delay);
    
    return forecasts;
});

// ?? Test telemetry endpoint
app.MapGet("/api/test-telemetry", async () =>
{
    using var activity = activitySource.StartActivity("TestTelemetryOperation");
    activity?.SetTag("operation.category", "testing");
    activity?.SetTag("custom.attribute", "dashboard_demo");
    
    var processingTime = Random.Shared.Next(25, 150);
    await Task.Delay(processingTime);
    
    activity?.SetTag("processing.completed", true);
    activity?.SetTag("processing.duration_ms", processingTime);
    
    return new { 
        Message = "?? Telemetry test successful", 
        ProcessingTimeMs = processingTime,
        Timestamp = DateTimeOffset.UtcNow,
        TraceId = activity?.TraceId.ToString()
    };
});

// ?? External call endpoint - Demonstrates distributed tracing
app.MapGet("/api/external-call", async (IHttpClientFactory httpClientFactory) =>
{
    using var activity = activitySource.StartActivity("ExternalServiceCall");
    activity?.SetTag("external.service", "httpbin");
    
    var httpClient = httpClientFactory.CreateClient();
    
    var response = await httpClient.GetAsync("https://httpbin.org/delay/1");
    var content = await response.Content.ReadAsStringAsync();
    
    activity?.SetTag("response.status", (int)response.StatusCode);
    activity?.SetTag("response.size", content.Length);
    
    return new { 
        Message = "?? External call completed", 
        StatusCode = (int)response.StatusCode,
        ResponseSize = content.Length,
        Timestamp = DateTimeOffset.UtcNow
    };
});

// ? Error endpoint - Demonstrates error tracking
app.MapGet("/api/error", () =>
{
    using var activity = activitySource.StartActivity("SimulateError");
    activity?.SetTag("error.type", "intentional_test");
    
    activity?.SetStatus(ActivityStatusCode.Error, "Intentional error for testing");
    throw new InvalidOperationException("?? Test error for dashboard demonstration");
});

// ?? Health check
app.MapHealthChecks("/health");

Console.WriteLine("?? API started with OpenTelemetry observability!");
Console.WriteLine();
Console.WriteLine("?? Test your dashboard:");
Console.WriteLine("?? GET /generate-traffic  - Creates diverse traffic for testing");
Console.WriteLine("??? GET /api/weather       - Business logic simulation");
Console.WriteLine("?? GET /api/test-telemetry - Custom telemetry demo");
Console.WriteLine("?? GET /api/external-call  - Distributed tracing demo");
Console.WriteLine("? GET /api/error         - Error tracking demo");
Console.WriteLine("?? GET /health            - Health check");

app.Run();