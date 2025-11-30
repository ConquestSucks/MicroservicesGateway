using System.Text.Json;
using ApiGateway.Models;
using Serilog;

namespace ApiGateway.Services;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;

    public UserServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("UserService");
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            Log.Warning("UserService returned status {StatusCode} for user {UserId}", response.StatusCode, userId);
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch user {UserId}", userId);
            return null;
        }
    }
}

