using ApiGateway.Middleware;

namespace ApiGateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Dictionary<string, RateLimitInfo> _rateLimitStore = new();

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        
        if (!_rateLimitStore.ContainsKey(clientId))
        {
            _rateLimitStore[clientId] = new RateLimitInfo { Count = 0, ResetTime = now.AddMinutes(1) };
        }
        
        var limitInfo = _rateLimitStore[clientId];
        
        if (now > limitInfo.ResetTime)
        {
            limitInfo.Count = 0;
            limitInfo.ResetTime = now.AddMinutes(1);
        }
        
        if (limitInfo.Count >= 100)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        limitInfo.Count++;
        await _next(context);
    }
}

