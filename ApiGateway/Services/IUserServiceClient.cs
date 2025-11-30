using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IUserServiceClient
{
    Task<User?> GetUserAsync(int userId);
}

