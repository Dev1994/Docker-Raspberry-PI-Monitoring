using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Create custom metrics
var meter = new Meter("YourWebApi.Metrics");
var requestCounter = meter.CreateCounter<int>("http_requests_total", "requests", "Total number of HTTP requests");
var requestDuration = meter.CreateHistogram<double>("http_request_duration_ms", "milliseconds", "Duration of HTTP requests");

// Create custom activity source for distributed tracing
var activitySource = new ActivitySource("YourWebApi.Activities");

// Configure OpenTelemetry
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
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) =>
            {
                // Don't trace health check endpoints
                return !httpContext.Request.Path.Value?.Contains("/health") == true;
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.RecordException = true;
        })
        .AddSource(activitySource.Name)
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddMeter(meter.Name)
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        }));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks();

// Register the meter and activity source for DI
builder.Services.AddSingleton(meter);
builder.Services.AddSingleton(activitySource);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom middleware to record metrics
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    var path = context.Request.Path.Value ?? "unknown";
    var method = context.Request.Method;

    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();
        
        // Record custom metrics
        requestCounter.Add(1, new KeyValuePair<string, object?>("method", method),
                              new KeyValuePair<string, object?>("path", path),
                              new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
        
        requestDuration.Record(stopwatch.ElapsedMilliseconds,
                              new KeyValuePair<string, object?>("method", method),
                              new KeyValuePair<string, object?>("path", path));
    }
});

app.UseRouting();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Sample endpoints
app.MapGet("/", () => "Hello World from .NET API with OpenTelemetry!");

app.MapGet("/api/weather", async () =>
{
    // Create a custom span for business logic
    using var activity = activitySource.StartActivity("GetWeatherData");
    activity?.SetTag("operation", "weather_forecast");
    
    // Simulate some work
    await Task.Delay(Random.Shared.Next(10, 100));
    
    var forecasts = new[]
    {
        new { Date = DateTime.Now.AddDays(1), TemperatureC = Random.Shared.Next(-20, 55), Summary = "Freezing" },
        new { Date = DateTime.Now.AddDays(2), TemperatureC = Random.Shared.Next(-20, 55), Summary = "Bracing" },
        new { Date = DateTime.Now.AddDays(3), TemperatureC = Random.Shared.Next(-20, 55), Summary = "Chilly" }
    };
    
    activity?.SetTag("forecast_count", forecasts.Length);
    return forecasts;
});

app.MapGet("/api/error", () =>
{
    using var activity = activitySource.StartActivity("SimulateError");
    activity?.SetStatus(ActivityStatusCode.Error, "Simulated error for testing");
    throw new InvalidOperationException("This is a test error for OpenTelemetry");
});

app.Run();app.Run();