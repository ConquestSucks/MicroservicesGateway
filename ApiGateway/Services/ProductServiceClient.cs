using System.Text.Json;
using ApiGateway.Models;
using Serilog;

namespace ApiGateway.Services;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ProductService");
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{productId}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            Log.Warning("ProductService returned status {StatusCode} for product {ProductId}", response.StatusCode, productId);
            return null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch product {ProductId}", productId);
            return null;
        }
    }

    public async Task<Dictionary<int, Product>> GetProductsAsync(IEnumerable<int> productIds)
    {
        var products = new Dictionary<int, Product>();
        
        foreach (var productId in productIds)
        {
            var product = await GetProductAsync(productId);
            if (product != null)
            {
                products[productId] = product;
            }
        }
        
        return products;
    }
}

