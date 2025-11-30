namespace ApiGateway.Middleware;

public class RateLimitInfo
{
    public int Count { get; set; }
    public DateTime ResetTime { get; set; }
}

