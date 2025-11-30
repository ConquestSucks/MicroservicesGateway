using OrderService.Data;

namespace OrderService.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/orders/user/{userId}", async (int userId, IOrderRepository repository) =>
        {
            var orders = await repository.GetByUserIdAsync(userId);
            return Results.Ok(orders);
        })
        .WithName("GetUserOrders")
        .WithOpenApi();

        app.MapGet("/api/orders/{id}", async (int id, IOrderRepository repository) =>
        {
            var order = await repository.GetByIdAsync(id);
            return order != null
                ? Results.Ok(order)
                : Results.NotFound(new { message = $"Order with id {id} not found" });
        })
        .WithName("GetOrder")
        .WithOpenApi();
    }
}

