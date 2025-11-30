using Prometheus;

namespace ProductService.Configuration;

public static class MonitoringConfiguration
{
    public static WebApplication UsePrometheusMetrics(this WebApplication app)
    {
        app.UseMetricServer();
        app.UseHttpMetrics();
        return app;
    }
}

