using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IProductServiceClient
{
    Task<Product?> GetProductAsync(int productId);
    Task<Dictionary<int, Product>> GetProductsAsync(IEnumerable<int> productIds);
}

