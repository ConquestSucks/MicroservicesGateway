using OrderService.Data;
using OrderService.Protos;
using Grpc.Core;

namespace OrderService.Services;

public class OrderGrpcService : Protos.OrderService.OrderServiceBase
{
    private readonly IOrderRepository _repository;

    public OrderGrpcService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public override async Task<GetUserOrdersResponse> GetUserOrders(GetUserOrdersRequest request, ServerCallContext context)
    {
        var orders = await _repository.GetByUserIdAsync(request.UserId);
        
        var response = new GetUserOrdersResponse();
        response.Orders.AddRange(orders.Select(o => new OrderResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            ProductId = o.ProductId,
            Quantity = o.Quantity,
            TotalPrice = (double)o.TotalPrice,
            OrderDate = o.OrderDate.ToString("O")
        }));

        return response;
    }

    public override async Task<OrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        var order = await _repository.GetByIdAsync(request.Id);
        
        if (order == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order with id {request.Id} not found"));
        }

        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            TotalPrice = (double)order.TotalPrice,
            OrderDate = order.OrderDate.ToString("O")
        };
    }
}

