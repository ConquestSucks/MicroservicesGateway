using ProductService.Data;

namespace ProductService.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products/{id}", async (int id, IProductRepository repository) =>
        {
            var product = await repository.GetByIdAsync(id);
            return product != null
                ? Results.Ok(product)
                : Results.NotFound(new { message = $"Product with id {id} not found" });
        })
        .WithName("GetProduct")
        .WithOpenApi();

        app.MapGet("/api/products", async (IProductRepository repository) =>
        {
            var products = await repository.GetAllAsync();
            return Results.Ok(products);
        })
        .WithName("GetAllProducts")
        .WithOpenApi();
    }
}

