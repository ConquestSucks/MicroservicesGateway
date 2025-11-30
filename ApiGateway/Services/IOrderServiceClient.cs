using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IOrderServiceClient
{
    Task<List<Order>> GetUserOrdersAsync(int userId);
}

