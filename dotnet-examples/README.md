# ?? OpenTelemetry with .NET Aspire - Complete Learning Example

> **?? Learning Goal**: Understand how to implement comprehensive observability in .NET applications using OpenTelemetry, complete with metrics, tracing, and beautiful dashboards.

This project demonstrates **production-ready observability** for a .NET Web API with:
- ?? **Metrics** - Performance and usage data per API endpoint
- ?? **Traces** - Request flows and timing analysis  
- ?? **Dashboards** - Beautiful Grafana visualizations
- ?? **Easy Setup** - Everything runs with one command using .NET Aspire

## ?? What is OpenTelemetry?

OpenTelemetry is the **industry standard** for collecting **observability data** from applications:

### **?? Metrics** (Numbers that matter)
- **Request rates** - How many requests per second?
- **Response times** - How fast is each endpoint?
- **Error rates** - What percentage of requests are failing?
- **Resource usage** - CPU, memory, garbage collection

### **?? Traces** (Request journeys) 
- **Request flow** - Path a request takes through your system
- **Timing breakdown** - How long each step takes
- **Error tracking** - Exactly where and when things fail
- **Distributed tracing** - Follow requests across multiple services

### **?? Logs** (Traditional text logging)
- Not implemented in this example, but easily added

## ??? Architecture Overview

```
???????????????????    ?? gRPC     ????????????????????????    ?? HTTP    ???????????????    ?? Queries    ???????????????
?   Your .NET API ? ???????????????? OpenTelemetry        ? ??????????????? Prometheus  ? ??????????????????   Grafana   ?
?                 ?                ? Collector            ?               ? (Metrics    ?                  ? (Beautiful  ?
? • Custom Spans  ?                ?                      ?               ?  Storage)   ?                  ?  Dashboard) ?
? • Route Metrics ?                ? • Receives Telemetry ?               ?             ?                  ?             ?
? • Error Tracking?                ? • Processes Data     ?               ???????????????                  ???????????????
???????????????????                ? • Exports to Backend?               
                                   ????????????????????????               
```

### **?? Data Flow**
1. **Your API** generates telemetry data (metrics + traces)
2. **OpenTelemetry Collector** receives and processes the data
3. **Prometheus** stores metrics in a time-series database
4. **Grafana** creates beautiful dashboards and alerts

## ?? Project Structure

```
??? Program.cs                     # ?? Web API with comprehensive OpenTelemetry setup
??? AppHost/
?   ??? AppHost.cs                 # ???  Aspire orchestration (starts all containers)
?   ??? otel-config.yml           # ??  OpenTelemetry Collector configuration  
?   ??? prometheus.yml             # ?? Prometheus scraping configuration
?   ??? grafana-datasources.yml   # ?? Grafana data source configuration
?   ??? dashboard-provider.yml     # ?? Dashboard auto-loading configuration
?   ??? api-dashboard.json         # ?? Pre-built production dashboard
??? docker-compose.reference.yml   # ?? Production deployment reference
??? README.md                      # ?? This comprehensive guide
```

## ?? Quick Start (5 Minutes to Full Observability!)

### **Prerequisites**
- ? **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download)
- ? **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop)

### **Step 1: Start Everything**
```bash
# Clone and navigate to the project
git clone <your-repo>
cd dotnet-examples

# Start the complete observability stack
cd AppHost
dotnet run
```

**?? That's it!** Wait 30-60 seconds for all containers to start.

### **Step 2: Access Your Tools**
- **?? Your API**: Check the Aspire dashboard for the dynamic port
- **??? Aspire Dashboard**: http://localhost:15888 (Container orchestration)
- **?? Prometheus**: http://localhost:9090 (Raw metrics)
- **?? Grafana**: http://localhost:3000 (Beautiful dashboards - login: admin/admin)

### **Step 3: Explore the Pre-Built Dashboard**

1. **Go to Grafana**: http://localhost:3000
2. **Login**: admin/admin (you'll be prompted to change this)
3. **Find the dashboard**: Look for **".NET API Dashboard"** in the side menu

You'll see a **production-ready dashboard** with:

#### **?? API Overview (Top Row)**
- **Requests/sec** - Real-time throughput
- **P95 Response Time** - 95th percentile latency  
- **Error Rate** - Percentage of failed requests
- **Active Requests** - Current concurrent load

#### **?? Request Analysis by Route** 
- **Request Rate by Route** - Traffic per API endpoint (`/api/weather`, `/api/error`, etc.)
- **Status Code Distribution** - Visual breakdown of HTTP response codes

#### **? Performance Metrics**
- **Total Requests by Route** - Cumulative usage per endpoint
- **Overall Response Time Percentiles** - P95, P50, and Average latencies
- **.NET GC Collections/sec** - Runtime performance monitoring
- **P95 Response Time by Route** - Performance breakdown per endpoint

### **Step 4: Generate Data and Watch the Magic!**

Hit different endpoints to see real-time data:

```bash
# Replace [port] with your API's actual port from Aspire dashboard
curl http://localhost:[port]/api/weather          # ? Normal business logic
curl http://localhost:[port]/api/test-telemetry   # ?? Custom tracing example
curl http://localhost:[port]/api/external-call    # ?? HTTP client tracing
curl http://localhost:[port]/api/error            # ? Error tracing
curl http://localhost:[port]/debug/telemetry      # ?? Debug information
```

**?? Watch the dashboard update in real-time!** Each endpoint will appear as separate lines in the charts.

## ?? Learning Exercises

### **Exercise 1: Understanding Custom Metrics**
1. **Visit** `/api/test-telemetry` multiple times
2. **Watch** the "Request Rate by Route" chart in Grafana
3. **Observe** how the `/api/test-telemetry` line increases

**?? Learning**: You're seeing custom metrics that track each endpoint individually.

### **Exercise 2: Distributed Tracing**
1. **Visit** `/api/external-call` (this makes an HTTP call to an external service)
2. **Go to** the Aspire dashboard traces section
3. **Find** traces that show the full request chain

**?? Learning**: You'll see how your request flows through multiple services with timing for each step.

### **Exercise 3: Error Monitoring**
1. **Visit** `/api/error` several times (this throws exceptions)
2. **Watch** the "Error Rate" stat turn red in Grafana  
3. **Check** the "Status Code Distribution" for 500 errors

**?? Learning**: See how errors are automatically captured and visualized.

### **Exercise 4: Performance Analysis**
1. **Generate mixed traffic** to different endpoints
2. **Compare** response times in "P95 Response Time by Route"
3. **Identify** which endpoints are fastest/slowest

**?? Learning**: Understand how to identify performance bottlenecks per endpoint.

## ?? Deep Dive: How It Works

### **?? Custom Route Metrics**
Our middleware captures detailed metrics for each endpoint:

```csharp
// This runs for every HTTP request
requestCounter.Add(1, new TagList
{
    { "route", "/api/weather" },      // Which endpoint
    { "method", "GET" },              // HTTP method  
    { "status_code", "200" }          // Response status
});
```

**Result**: Grafana can show separate lines for `/api/weather`, `/api/error`, etc.

### **?? Custom Tracing Spans**
Business logic gets detailed tracing:

```csharp
using var activity = activitySource.StartActivity("GenerateWeatherForecast");
activity?.SetTag("operation.type", "weather_forecast");
activity?.SetTag("forecast.count", forecasts.Length);
```

**Result**: Distributed traces show your custom business operations with timing.

### **?? Dashboard Queries**
The Grafana dashboard uses PromQL queries like:

```promql
# Request rate per endpoint
sum by(route) (rate(http_requests_total[1m]))

# P95 response time
histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[1m])) by (le))
```

**Result**: Rich visualizations that update in real-time.

## ??? Customization Guide

### **Adding New Metrics**
```csharp
// 1. Define a new metric
var businessMetric = customMeter.CreateCounter<long>("business_operations_total");

// 2. Record data
businessMetric.Add(1, new TagList { { "operation", "user_signup" } });

// 3. Query in Grafana: business_operations_total
```

### **Adding Custom Spans**
```csharp
// Create detailed tracing for any operation
using var activity = activitySource.StartActivity("ProcessPayment");
activity?.SetTag("payment.amount", 99.99);
activity?.SetTag("payment.method", "credit_card");
// Your payment logic here...
```

### **Creating New Dashboard Panels**
1. **Go to Grafana** ? Create ? Dashboard
2. **Add a panel** with a PromQL query
3. **Example query**: `sum by(route) (rate(http_requests_total[1m]))`
4. **Customize** visualization, thresholds, and alerts

## ?? Production Deployment

For production, see `docker-compose.reference.yml` for a complete setup including:
- **?? Security** - Proper authentication and TLS
- **?? Persistence** - Data storage volumes  
- **?? Scaling** - Multiple collector instances
- **?? Alerting** - Automated monitoring alerts

## ?? Key Learning Outcomes

After completing this example, you'll understand:

? **How to implement OpenTelemetry** in .NET applications  
? **Custom metrics vs automatic metrics** and when to use each  
? **Distributed tracing** for request flow analysis  
? **Dashboard creation** for production monitoring  
? **Performance monitoring** techniques for APIs  
? **Error tracking and alerting** best practices  
? **Production deployment** considerations  

## ?? Troubleshooting

### **Dashboard Not Loading?**
```bash
# Check if all containers are running
docker ps

# Restart if needed
cd AppHost && dotnet run
```

### **No Metrics Appearing?**
1. **Check** `/debug/collector` endpoint
2. **Verify** OpenTelemetry Collector is reachable
3. **Wait** 30-60 seconds for data to appear

### **Can't Access Grafana?**
- **URL**: http://localhost:3000
- **Login**: admin/admin
- **Wait** for container to fully start (check Docker Desktop)

## ?? Next Steps

1. **?? Experiment** with custom metrics for your business logic
2. **?? Create** additional dashboard panels for specific KPIs  
3. **?? Set up** alerting rules for critical thresholds
4. **?? Explore** distributed tracing across multiple services
5. **?? Read** [OpenTelemetry documentation](https://opentelemetry.io/docs/) for advanced concepts

---

**?? Congratulations!** You now have a complete, production-ready observability setup that you can adapt for any .NET application. Happy monitoring! ???