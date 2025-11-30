using ApiGateway.Services;
using Microsoft.Extensions.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Serilog;
using StackExchange.Redis;

namespace ApiGateway.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IDatabase>(sp =>
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        return services;
    }

    public static IServiceCollection AddHttpClientsWithPolly(this IServiceCollection services, IConfiguration configuration)
    {
        var userServiceUrl = configuration["Services:UserService"] ?? "http://localhost:5001";
        var orderServiceUrl = configuration["Services:OrderService"] ?? "http://localhost:5002";
        var productServiceUrl = configuration["Services:ProductService"] ?? "http://localhost:5003";

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Log.Warning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (result, duration) =>
                {
                    Log.Error("Circuit breaker opened for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    Log.Information("Circuit breaker reset");
                });

        var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

        services.AddHttpClient("UserService", client =>
        {
            client.BaseAddress = new Uri(userServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        }).AddPolicyHandler(policyWrap);

        services.AddHttpClient("OrderService", client =>
        {
            client.BaseAddress = new Uri(orderServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        }).AddPolicyHandler(policyWrap);

        services.AddHttpClient("ProductService", client =>
        {
            client.BaseAddress = new Uri(productServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        }).AddPolicyHandler(policyWrap);

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var useGrpc = configuration.GetValue<bool>("Services:UseGrpc", true);
        
        if (useGrpc)
        {
            services.AddScoped<IUserServiceClient>(sp => 
                new UserServiceGrpcClient(configuration, Log.Logger));
            services.AddScoped<IOrderServiceClient>(sp => 
                new OrderServiceGrpcClient(configuration, Log.Logger));
            services.AddScoped<IProductServiceClient>(sp => 
                new ProductServiceGrpcClient(configuration, Log.Logger));
        }
        else
        {
            services.AddScoped<IUserServiceClient, UserServiceClient>();
            services.AddScoped<IOrderServiceClient, OrderServiceClient>();
            services.AddScoped<IProductServiceClient, ProductServiceClient>();
        }
        
        services.AddScoped<IProfileAggregationService, ProfileAggregationService>();
        
        return services;
    }
}

