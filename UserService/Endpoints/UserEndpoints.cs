using UserService.Data;

namespace UserService.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users/{id}", async (int id, IUserRepository repository) =>
        {
            var user = await repository.GetByIdAsync(id);
            return user != null
                ? Results.Ok(user)
                : Results.NotFound(new { message = $"User with id {id} not found" });
        })
        .WithName("GetUser")
        .WithOpenApi();

        app.MapGet("/api/users", async (IUserRepository repository) =>
        {
            var users = await repository.GetAllAsync();
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .WithOpenApi();
    }
}

