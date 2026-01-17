# Connecting External .NET Containers to OpenTelemetry

Your .NET application is running in a separate Docker project/container. Here are the connection options:

## Option 1: Host Network Connection (Current Setup ✅)

**Works immediately** - Your OTEL Collector exposes ports 4317/4318 to the host.

### In your .NET container's docker-compose.yml or run command:

```yaml
# docker-compose.yml for your .NET app
version: "3.8"
services:
  your-dotnet-api:
    build: .
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://host.docker.internal:4317  # Windows/Mac
      # OR for Linux:
      # - OTEL_EXPORTER_OTLP_ENDPOINT=http://172.17.0.1:4317
      # OR use your actual host IP:
      # - OTEL_EXPORTER_OTLP_ENDPOINT=http://192.168.1.100:4317
```

### In your .NET appsettings.json:
```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://host.docker.internal:4317"
  }
}
```

## Option 2: Shared Docker Network (Recommended)

**Best for production** - Containers communicate directly without going through host.

### Step 1: Create the shared network
```bash
docker network create monitoring
```

### Step 2: Connect your .NET container
```yaml
# Your .NET app's docker-compose.yml
version: "3.8"
services:
  your-dotnet-api:
    build: .
    networks:
      - monitoring  # Same network as OTEL Collector
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://monitoring-otel-collector:4317

networks:
  monitoring:
    external: true  # Use the external network
```

### Step 3: Update monitoring stack
```bash
cd /path/to/monitoring-stack
docker-compose down
docker-compose up -d
```

## Option 3: External Network (Alternative)

Create an external network that both stacks can join:

```bash
# Create external network
docker network create monitoring-external

# Add to your monitoring stack docker-compose.yml
networks:
  monitoring-external:
    external: true

# Add to your .NET app docker-compose.yml  
networks:
  monitoring-external:
    external: true
```

## Testing the Connection

### 1. Check if OTEL Collector is reachable:
```bash
# From your .NET container
curl http://host.docker.internal:4317
# OR
curl http://monitoring-otel-collector:4317  # if using shared network
```

### 2. Check OTEL Collector logs:
```bash
docker logs monitoring-otel-collector
```

### 3. Verify metrics in Prometheus:
- Visit http://localhost:9090
- Search for metrics with your service name

## Environment Variables for .NET Container

Set these in your .NET container environment:

```bash
# Service identification
OTEL_SERVICE_NAME=your-api-name
OTEL_SERVICE_VERSION=1.0.0

# Endpoint (choose one based on your setup)
OTEL_EXPORTER_OTLP_ENDPOINT=http://host.docker.internal:4317        # Host network
# OR
OTEL_EXPORTER_OTLP_ENDPOINT=http://monitoring-otel-collector:4317   # Shared network

# Optional: Resource attributes
OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production,service.instance.id=api-1
```

## Troubleshooting

### Connection Issues:
1. **Check network connectivity**: `docker network ls`
2. **Verify container can reach collector**: `docker exec -it your-container ping monitoring-otel-collector`
3. **Check exposed ports**: `docker port monitoring-otel-collector`
4. **Review OTEL logs**: `docker logs monitoring-otel-collector`

### Common Issues:
- **Host network on Linux**: Use `172.17.0.1` instead of `host.docker.internal`
- **Firewall**: Ensure ports 4317/4318 are open
- **Network isolation**: Make sure both containers can communicate

## Recommended Approach

**For development**: Use Option 1 (host network) - it's simple and works immediately.

**For production**: Use Option 2 (shared network) - it's more secure and performant.

The current setup supports both approaches!