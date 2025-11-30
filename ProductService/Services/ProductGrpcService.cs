using ProductService.Data;
using ProductService.Protos;
using Grpc.Core;

namespace ProductService.Services;

public class ProductGrpcService : Protos.ProductService.ProductServiceBase
{
    private readonly IProductRepository _repository;

    public ProductGrpcService(IProductRepository repository)
    {
        _repository = repository;
    }

    public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var product = await _repository.GetByIdAsync(request.Id);
        
        if (product == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with id {request.Id} not found"));
        }

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = (double)product.Price
        };
    }

    public override async Task<GetAllProductsResponse> GetAllProducts(GetAllProductsRequest request, ServerCallContext context)
    {
        var products = await _repository.GetAllAsync();
        
        var response = new GetAllProductsResponse();
        response.Products.AddRange(products.Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = (double)p.Price
        }));

        return response;
    }
}

