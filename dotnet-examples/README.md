# .NET Web API OpenTelemetry Configuration

To configure your .NET Web API to send telemetry to the OpenTelemetry Collector, follow these steps:

## 1. Install Required NuGet Packages

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.SqlClient
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
```

## 2. Configure Program.cs (Minimal API / .NET 6+)

See the example `Program.cs` file in this directory.

## 3. Configuration Settings

Add the following to your `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "ServiceName": "YourWebApiName",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

## 4. Environment Variables (for Docker deployment)

When deploying your .NET API, set these environment variables:

```bash
OTEL_SERVICE_NAME=YourWebApiName
OTEL_SERVICE_VERSION=1.0.0
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

## 5. Custom Metrics Example

The example also shows how to create custom metrics and activity sources for your specific business logic.

## 6. Running with Docker

Make sure your .NET API can reach the OpenTelemetry Collector. If both are in Docker:

1. Add your .NET API to the same network as the monitoring stack
2. Use `otel-collector:4317` as the OTLP endpoint
3. Or use `host.docker.internal:4317` if running .NET API outside Docker

## Available Ports

- **4317**: OTLP gRPC endpoint (recommended for .NET)
- **4318**: OTLP HTTP endpoint (alternative)
- **8889**: Prometheus metrics endpoint (scraped by Prometheus)