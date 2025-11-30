using Prometheus;

namespace ApiGateway.Configuration;

public static class MonitoringConfiguration
{
    public static WebApplication UsePrometheusMetrics(this WebApplication app)
    {
        app.UseMetricServer();
        app.UseHttpMetrics(options =>
        {
            options.RequestCount.Enabled = true;
            options.RequestDuration.Enabled = true;
        });
        
        return app;
    }
}

