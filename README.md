# Raspberry Pi & Docker Monitoring

## Hit the Star! :star:

If you find this repository useful, please consider giving it a star. Your support is greatly appreciated! :pray:

## Introduction

Introducing the comprehensive Raspberry Pi and Application monitoring solution using Grafana, Prometheus, OpenTelemetry, Cadvisor, and Node-Exporter Stack! This project provides a complete monitoring platform for your Raspberry Pi infrastructure and .NET applications. With Grafana's intuitive dashboards, you can visualize system metrics collected by Prometheus and Cadvisor, hardware information from Node-Exporter, and application telemetry data from your .NET Web APIs through OpenTelemetry. The combination of these tools results in a powerful and efficient monitoring solution that gives you complete visibility into your system's health, application performance, and business metrics.

This repository contains a `docker-compose` file to run a complete monitoring stack. It is based on the following projects:
- [Prometheus](https://prometheus.io/) - Metrics collection and storage
- [Grafana](http://grafana.org/) - Visualization and dashboards
- [OpenTelemetry Collector](https://opentelemetry.io/) - Application observability and telemetry
- [cAdvisor](https://github.com/google/cadvisor) - Container monitoring
- [NodeExporter](https://github.com/prometheus/node_exporter) - Hardware and OS metrics

## Prerequisites

Before we get started installing the stack, we need to make sure that the following prerequisites are met:
- Docker is installed on the host machine
- Docker Compose is installed on the host machine
- The host machine is running a Raspberry PI OS or any other compatible Linux distribution

## Installation and Configuration

To install the stack, follow the steps below:

- Clone this repository to your host machine.
```bash
git clone https://github.com/oijkn/Docker-Raspberry-PI-Monitoring.git
```

- Enter to the cloned directory.
```bash
cd Docker-Raspberry-PI-Monitoring
```

 - Create `data` directory and change the ownership of the `prometheus` and `grafana` folders for a nice and clean installation.
```bash
mkdir -p prometheus/data grafana/data && \
sudo chown -R 472:472 grafana/ && \
sudo chown -R 65534:65534 prometheus/
```

 - Start the stack with `docker-compose`.
```bash
docker-compose up -d
```

This will start all the containers and make them available on the host machine.
<br/>The following ports are used:
- **3000**: Grafana (Web UI - exposed to host)
- **4317**: OpenTelemetry Collector gRPC endpoint (exposed to host for .NET APIs)
- **4318**: OpenTelemetry Collector HTTP endpoint (exposed to host for .NET APIs)
- 9090: Prometheus (internal)
- 8080: cAdvisor (internal)
- 9100: NodeExporter (internal)
- 8888: OpenTelemetry Collector internal metrics (internal)
- 8889: OpenTelemetry Collector Prometheus exporter (internal)

The Grafana dashboard can be accessed by navigating to `http://<host-ip>:3000` in your browser for example `http://192.168.1.100:3000`.
<br/>The default username and password are both `admin`. You will be prompted to change the password on the first login.
<br/>Credentials can be changed by editing the [.env](grafana/.env) file.

If you would like to change which targets should be monitored, you can edit the [prometheus.yml](prometheus/prometheus.yml) file.
<br/>The targets section contains a list of all the targets that should be monitored by Prometheus.
<br/>The names defined in the `job_name` section are used to identify the targets in Grafana.
<br/>The `static_configs` section contains the IP addresses of the targets that should be monitored. Actually, they are sourced from the service names defined in the [docker-compose.yml](docker-compose.yml) file.
<br/>If you think that the `scrape_interval` value is too aggressive, you can change it to a more suitable value.

In order to check if the stack is running correctly, you can run the following command:
```bash
docker compose ps
```

View the logs of a specific container by running the following command:
```bash
docker logs -f <container-name>
```

**Note**: After making configuration changes, restart the stack:
```bash
sudo docker compose down && sudo docker compose up -d
```

## .NET Web API Monitoring with OpenTelemetry

This stack includes OpenTelemetry Collector support for monitoring .NET Web APIs with distributed tracing, custom metrics, and comprehensive observability.

### Quick Setup for .NET APIs

1. **Install Required NuGet Packages** in your .NET Web API:
```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

2. **Configure OpenTelemetry** in your `Program.cs` (see [complete example](dotnet-examples/Program.cs)):
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

3. **Configure OTLP Endpoint** (choose based on your deployment):
   - **Local development**: `http://localhost:4317`
   - **Same Docker network**: `http://otel-collector:4317`
   - **External Docker containers**: `http://host.docker.internal:4317` (Windows/Mac) or `http://172.17.0.1:4317` (Linux)
   - **External APIs on different hosts**: `http://YOUR_HOST_IP:4317`

### Features Included

✅ **Distributed Tracing** - Track requests across services  
✅ **Custom Metrics** - Business-specific measurements  
✅ **Automatic Instrumentation** - ASP.NET Core, HTTP, SQL monitoring  
✅ **Error Tracking** - Exception monitoring and alerting  
✅ **Performance Monitoring** - Request duration, throughput, etc.

### Complete Examples

Check the [dotnet-examples/](dotnet-examples/) directory for:
- Complete `Program.cs` configuration
- Custom metrics and tracing examples
- Configuration settings
- Docker deployment guidance

### External Containers Support

🔗 **Your .NET application runs in a separate Docker project?** No problem!

The OpenTelemetry Collector is configured to accept connections from external containers through:
- **Host network exposure**: Ports 4317/4318 are exposed to the host
- **Shared network support**: Join the `monitoring` network for direct container communication

**Quick connection options**:
1. **Host network** (works immediately): `http://host.docker.internal:4317`
2. **Shared network** (recommended for production): `http://monitoring-otel-collector:4317`

For detailed setup instructions, see [External Container Setup Guide](dotnet-examples/External-Container-Setup.md).

## Add Data Sources and Dashboards

Since Grafana v5 has introduced the concept of provisioning, it is possible to automatically add data sources and dashboards to Grafana.
<br/>This is done by placing the `datasources` and `dashboards` directories in the [provisioning](grafana/provisioning) folder. The files in these directories are automatically loaded by Grafana on startup.

If you like to add a new dashboard, simply place the JSON file in the [dashboards](grafana/provisioning/dashboards) directory, and it will be automatically loaded next time Grafana is started.

# Install Dashboard from Grafana.com (Optional)

If you would like to install this dashboard from Grafana.com, simply follow the steps below:
- Navigate to the dashboard on [Grafana.com Dashboard](https://grafana.com/grafana/dashboards/15120-raspberry-pi-docker-monitoring/)
- Click on the `Copy ID to Clipboard` button
- Navigate to the `Import` page in Grafana
- Paste the ID into the `Import via grafana.com` field
- Click on the `Load` button
- Click on the `Import` button

Or you can follow the steps described in the [Grafana Documentation](https://grafana.com/docs/grafana/latest/dashboards/manage-dashboards/#import-a-dashboard).

This dashboard is intended to help you get started with monitoring your Raspberry PI devices. If you have any changes or suggestions, you would like to see, please feel free to open an issue or create a pull request.

Here is a screenshot of the dashboard:
![Grafana Dashboard](grafana/screenshots/dashboard.png)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Troubleshooting

Enable `c-group` memory and swap accounting on the host machine by running the following command:
```bash
sudo sed -i 's/^GRUB_CMDLINE_LINUX=""/GRUB_CMDLINE_LINUX="cgroup_enable=cpuset cgroup_enable=memory cgroup_memory=1 swapaccount=1"/' /etc/default/grub
sudo update-grub
sudo reboot
```
