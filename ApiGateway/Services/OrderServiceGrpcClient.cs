using ApiGateway.Models;
using Grpc.Net.Client;
using Serilog;
using OrderService.Protos;

namespace ApiGateway.Services;

public class OrderServiceGrpcClient : IOrderServiceClient
{
    private readonly OrderService.Protos.OrderService.OrderServiceClient _client;
    private readonly Serilog.ILogger _logger;

    public OrderServiceGrpcClient(IConfiguration configuration, Serilog.ILogger logger)
    {
        var orderServiceUrl = configuration["Services:OrderServiceGrpc"] ?? "http://localhost:5002";
        var channel = GrpcChannel.ForAddress(orderServiceUrl);
        _client = new OrderService.Protos.OrderService.OrderServiceClient(channel);
        _logger = logger;
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        try
        {
            var request = new GetUserOrdersRequest { UserId = userId };
            var response = await _client.GetUserOrdersAsync(request);
            
            return response.Orders.Select(o => new Order(
                o.Id,
                o.UserId,
                o.ProductId,
                o.Quantity,
                (decimal)o.TotalPrice,
                DateTime.Parse(o.OrderDate)
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to fetch orders for user {UserId} via gRPC", userId);
            return new List<Order>();
        }
    }
}

