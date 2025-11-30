using ApiGateway.Models;
using Grpc.Net.Client;
using Serilog;
using ProductService.Protos;

namespace ApiGateway.Services;

public class ProductServiceGrpcClient : IProductServiceClient
{
    private readonly ProductService.Protos.ProductService.ProductServiceClient _client;
    private readonly Serilog.ILogger _logger;

    public ProductServiceGrpcClient(IConfiguration configuration, Serilog.ILogger logger)
    {
        var productServiceUrl = configuration["Services:ProductServiceGrpc"] ?? "http://localhost:5003";
        var channel = GrpcChannel.ForAddress(productServiceUrl);
        _client = new ProductService.Protos.ProductService.ProductServiceClient(channel);
        _logger = logger;
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        try
        {
            var request = new GetProductRequest { Id = productId };
            var response = await _client.GetProductAsync(request);
            
            return new Product(
                response.Id,
                response.Name,
                response.Description,
                (decimal)response.Price
            );
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.Warning("Product {ProductId} not found via gRPC", productId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to fetch product {ProductId} via gRPC", productId);
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

