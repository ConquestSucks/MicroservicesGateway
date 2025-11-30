using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Polly.CircuitBreaker;

namespace ApiGateway.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        app.MapGet("/api/profile/{userId}", async (
            int userId,
            IProfileAggregationService profileService) =>
        {
            try
            {
                var profile = await profileService.GetUserProfileWithFallbackAsync(userId);
                
                if (profile == null)
                {
                    return Results.NotFound(new { message = $"User {userId} not found" });
                }
                
                if (profile.User == null && profile.Orders.Count > 0)
                {
                    return Results.Ok(profile);
                }
                
                return Results.Ok(profile);
            }
            catch (BrokenCircuitException ex)
            {
                Log.Error(ex, "Circuit breaker is open for user {UserId}", userId);
                return Results.StatusCode(503);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching profile for user {UserId}", userId);
                return Results.Problem("An error occurred while fetching the profile");
            }
        })
        .WithName("GetUserProfile")
        .WithOpenApi()
        .RequireAuthorization();
    }
}

