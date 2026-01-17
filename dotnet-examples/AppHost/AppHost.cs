var builder = DistributedApplication.CreateBuilder(args);

// ================================
// ?? OPENTELEMETRY COLLECTOR  
// ================================
// The collector is the central hub that:
// • Receives telemetry data from your application (via gRPC/HTTP)
// • Processes and transforms the data
// • Exports it to various backends (Prometheus, Jaeger, etc.)

var otel = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.114.0")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")    // ?? gRPC endpoint for telemetry data
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")    // ?? HTTP endpoint for telemetry data  
    .WithHttpEndpoint(port: 8889, targetPort: 8889, name: "prometheus")   // ?? Prometheus metrics scraping endpoint
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "otel-config.yml"), "/etc/otelcol-contrib/otel-collector-config.yml")
    .WithArgs("--config=/etc/otelcol-contrib/otel-collector-config.yml");

// ================================  
// ?? PROMETHEUS (METRICS STORAGE)
// ================================
// Prometheus is a time-series database that:
// • Scrapes metrics from the OpenTelemetry Collector every 15 seconds
// • Stores them in an efficient time-series format
// • Provides PromQL query language for data analysis
// • Serves as data source for Grafana dashboards

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.0.1")
    .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "prometheus-ui")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "prometheus.yml"), "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", 
              "--storage.tsdb.path=/prometheus", 
              "--web.console.libraries=/etc/prometheus/console_libraries", 
              "--web.console.templates=/etc/prometheus/consoles", 
              "--web.enable-lifecycle");

// ================================
// ?? GRAFANA (VISUALIZATION DASHBOARDS)
// ================================  
// Grafana provides beautiful dashboards that:
// • Connect to Prometheus as a data source
// • Automatically load our pre-built API monitoring dashboard
// • Provide real-time visualizations with proper thresholds
// • Support alerting and advanced analytics

var grafana = builder.AddContainer("grafana", "grafana/grafana", "11.4.0")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "grafana-ui")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")  // ?? Default login: admin/admin
    .WithEnvironment("GF_PATHS_PROVISIONING", "/etc/grafana/provisioning")
    // ?? Auto-configure Prometheus as data source
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "grafana-datasources.yml"), "/etc/grafana/provisioning/datasources/datasources.yml")
    // ?? Auto-load dashboard configuration
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "dashboard-provider.yml"), "/etc/grafana/provisioning/dashboards/dashboards.yml")
    // ?? Pre-built production-ready API dashboard
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "api-dashboard.json"), "/var/lib/grafana/dashboards/api-dashboard.json");

// ================================
// ?? YOUR WEB API APPLICATION
// ================================
// The API is configured with OpenTelemetry environment variables
// It will automatically send telemetry data to the OpenTelemetry Collector

var api = builder.AddProject<Projects.YourWebApi>("webapi")
    // ??? Service identification for distributed tracing
    .WithEnvironment("OpenTelemetry:ServiceName", "YourWebApi")
    .WithEnvironment("OpenTelemetry:ServiceVersion", "1.0.0")
    
    // ?? OpenTelemetry Collector endpoints  
    .WithEnvironment("OpenTelemetry:OtlpEndpoint", "http://localhost:4317")      // gRPC endpoint (preferred)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")    // Standard OpenTelemetry env var
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")                    // Use gRPC (more reliable than HTTP)
    
    // ?? Additional telemetry configuration
    .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "service.name=YourWebApi,service.version=1.0.0")
    .WithEnvironment("OTEL_LOG_LEVEL", "debug");                               // Verbose logging for learning

// ================================
// ? SERVICE DEPENDENCIES  
// ================================
// Ensure the API waits for the OpenTelemetry Collector to be ready
// This prevents telemetry connection errors during startup

api.WaitFor(otel);

// ?? Start the complete observability stack!
Console.WriteLine("?? Starting complete observability stack...");
Console.WriteLine("?? OpenTelemetry Collector will start first");  
Console.WriteLine("?? Then Prometheus and Grafana");
Console.WriteLine("?? Finally your API with full telemetry");
Console.WriteLine();
Console.WriteLine("?? After startup, visit:");
Console.WriteLine("?? Grafana Dashboard: http://localhost:3000 (admin/admin)");
Console.WriteLine("?? Prometheus: http://localhost:9090");
Console.WriteLine("??? Aspire Dashboard: http://localhost:15888");

builder.Build().Run();
