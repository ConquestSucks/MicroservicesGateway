using ApiGateway.Models;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;
using Polly.CircuitBreaker;

namespace ApiGateway.Services;

public class ProfileAggregationService : IProfileAggregationService
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly IOrderServiceClient _orderServiceClient;
    private readonly IProductServiceClient _productServiceClient;
    private readonly IDatabase _cache;

    public ProfileAggregationService(
        IUserServiceClient userServiceClient,
        IOrderServiceClient orderServiceClient,
        IProductServiceClient productServiceClient,
        IDatabase cache)
    {
        _userServiceClient = userServiceClient;
        _orderServiceClient = orderServiceClient;
        _productServiceClient = productServiceClient;
        _cache = cache;
    }

    public async Task<UserProfile?> GetUserProfileAsync(int userId)
    {
        var cacheKey = $"profile:{userId}";
        
        var cachedData = await _cache.StringGetAsync(cacheKey);
        if (cachedData.HasValue)
        {
            Log.Information("Cache hit for user {UserId}", userId);
            return JsonSerializer.Deserialize<UserProfile>(cachedData!);
        }
        
        Log.Information("Cache miss for user {UserId}, fetching from services", userId);
        
        try
        {
            var userTask = _userServiceClient.GetUserAsync(userId);
            var ordersTask = _orderServiceClient.GetUserOrdersAsync(userId);
            
            await Task.WhenAll(userTask, ordersTask);
            
            var user = await userTask;
            var orders = await ordersTask;
            
            if (user == null)
            {
                return null;
            }
            
            var productIds = orders.Select(o => o.ProductId).Distinct();
            var products = await _productServiceClient.GetProductsAsync(productIds);
            
            var enrichedOrders = orders.Select(o => new OrderProfileItem
            {
                Id = o.Id,
                UserId = o.UserId,
                Product = products.TryGetValue(o.ProductId, out var p) ? p : null,
                Quantity = o.Quantity,
                TotalPrice = o.TotalPrice,
                OrderDate = o.OrderDate
            }).ToList();
            
            var profile = new UserProfile
            {
                User = user,
                Orders = enrichedOrders,
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalPrice)
            };
            
            var profileJson = JsonSerializer.Serialize(profile);
            await _cache.StringSetAsync(cacheKey, profileJson, TimeSpan.FromSeconds(30));
            
            Log.Information("Profile data cached for user {UserId}", userId);
            
            return profile;
        }
        catch (BrokenCircuitException ex)
        {
            Log.Error(ex, "Circuit breaker is open for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfile?> GetUserProfileWithFallbackAsync(int userId)
    {
        var cacheKey = $"profile:{userId}";
        
        var cachedData = await _cache.StringGetAsync(cacheKey);
        if (cachedData.HasValue)
        {
            Log.Information("Cache hit for user {UserId}", userId);
            return JsonSerializer.Deserialize<UserProfile>(cachedData!);
        }
        
        Log.Information("Cache miss for user {UserId}, fetching from services with fallback", userId);
        
        User? user = null;
        List<Models.Order> orders = new();
        var hasPartialData = false;
        
        try
        {
            var userTask = _userServiceClient.GetUserAsync(userId);
            var ordersTask = _orderServiceClient.GetUserOrdersAsync(userId);
            
            await Task.WhenAll(userTask, ordersTask);
            
            user = await userTask;
            orders = await ordersTask;
            
            if (user != null || orders.Count > 0)
            {
                hasPartialData = true;
            }
            
            if (!hasPartialData)
            {
                Log.Warning("All services failed for user {UserId}", userId);
                return null;
            }
            
            if (user == null && orders.Count > 0)
            {
                Log.Warning("User {UserId} not found but orders exist. Returning partial data.", userId);
                var partialProductIds = orders.Select(o => o.ProductId).Distinct().ToList();
                var partialProducts = await _productServiceClient.GetProductsAsync(partialProductIds);
                
                var partialEnrichedOrders = orders.Select(o => new OrderProfileItem
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    Product = partialProducts.TryGetValue(o.ProductId, out var p) ? p : null,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate
                }).ToList();
                
                var partialProfile = new UserProfile
                {
                    User = null,
                    Orders = partialEnrichedOrders,
                    TotalOrders = orders.Count,
                    TotalSpent = orders.Sum(o => o.TotalPrice)
                };
                
                var partialProfileJson = JsonSerializer.Serialize(partialProfile);
                await _cache.StringSetAsync(cacheKey, partialProfileJson, TimeSpan.FromSeconds(15));
                
                return partialProfile;
            }
            
            var mainProductIds = orders.Select(o => o.ProductId).Distinct().ToList();
            var mainProducts = await _productServiceClient.GetProductsAsync(mainProductIds);
            
            var mainEnrichedOrders = orders.Select(o => new OrderProfileItem
            {
                Id = o.Id,
                UserId = o.UserId,
                Product = mainProducts.TryGetValue(o.ProductId, out var p) ? p : null,
                Quantity = o.Quantity,
                TotalPrice = o.TotalPrice,
                OrderDate = o.OrderDate
            }).ToList();
            
            var profile = new UserProfile
            {
                User = user,
                Orders = mainEnrichedOrders,
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalPrice)
            };
            
            var mainProfileJson = JsonSerializer.Serialize(profile);
            await _cache.StringSetAsync(cacheKey, mainProfileJson, TimeSpan.FromSeconds(30));
            
            Log.Information("Profile data cached for user {UserId}", userId);
            
            return profile;
        }
        catch (BrokenCircuitException ex)
        {
            Log.Error(ex, "Circuit breaker is open for user {UserId}. Attempting fallback.", userId);
            
            if (user != null || orders.Count > 0)
            {
                var fallbackProductIds = orders.Select(o => o.ProductId).Distinct().ToList();
                var fallbackProducts = await _productServiceClient.GetProductsAsync(fallbackProductIds);
                
                var fallbackEnrichedOrders = orders.Select(o => new OrderProfileItem
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    Product = fallbackProducts.TryGetValue(o.ProductId, out var p) ? p : null,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate
                }).ToList();
                
                var fallbackProfile = new UserProfile
                {
                    User = user,
                    Orders = fallbackEnrichedOrders,
                    TotalOrders = orders.Count,
                    TotalSpent = orders.Sum(o => o.TotalPrice)
                };
                
                Log.Information("Returning fallback profile for user {UserId}", userId);
                return fallbackProfile;
            }
            
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching profile for user {UserId}", userId);
            
            if (user != null || orders.Count > 0)
            {
                var fallbackProductIds = orders.Select(o => o.ProductId).Distinct().ToList();
                var fallbackProducts = await _productServiceClient.GetProductsAsync(fallbackProductIds);
                
                var fallbackEnrichedOrders = orders.Select(o => new OrderProfileItem
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    Product = fallbackProducts.TryGetValue(o.ProductId, out var p) ? p : null,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate
                }).ToList();
                
                var fallbackProfile = new UserProfile
                {
                    User = user,
                    Orders = fallbackEnrichedOrders,
                    TotalOrders = orders.Count,
                    TotalSpent = orders.Sum(o => o.TotalPrice)
                };
                
                Log.Information("Returning fallback profile for user {UserId} with partial data", userId);
                return fallbackProfile;
            }
            
            throw;
        }
    }
}

