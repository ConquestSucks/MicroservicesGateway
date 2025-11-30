using System.Text.Json;
using ApiGateway.Models;
using Serilog;

namespace ApiGateway.Services;

public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;

    public OrderServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OrderService");
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/orders/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Order>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Order>();
            }
            
            Log.Warning("OrderService returned status {StatusCode} for user {UserId}", response.StatusCode, userId);
            return new List<Order>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch orders for user {UserId}", userId);
            return new List<Order>();
        }
    }
}

