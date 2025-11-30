using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IProfileAggregationService
{
    Task<UserProfile?> GetUserProfileAsync(int userId);
    Task<UserProfile?> GetUserProfileWithFallbackAsync(int userId);
}

