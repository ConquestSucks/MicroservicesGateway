using OrderService.Models;

namespace OrderService.Data;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<int, List<Order>> _orders = new()
    {
        { 1, new List<Order> 
            { 
                new Order(1, 1, 101, 2, 1999.99m, DateTime.UtcNow.AddDays(-5)), 
                new Order(2, 1, 102, 1, 899.99m, DateTime.UtcNow.AddDays(-2)) 
            } 
        },
        { 2, new List<Order> 
            { 
                new Order(3, 2, 103, 3, 2999.97m, DateTime.UtcNow.AddDays(-10)) 
            } 
        },
        { 3, new List<Order> 
            { 
                new Order(4, 3, 101, 1, 999.99m, DateTime.UtcNow.AddDays(-1)) 
            } 
        }
    };

    public Task<Order?> GetByIdAsync(int id)
    {
        var order = _orders.Values
            .SelectMany(o => o)
            .FirstOrDefault(o => o.Id == id);
        
        return Task.FromResult(order);
    }

    public Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
    {
        _orders.TryGetValue(userId, out var userOrders);
        return Task.FromResult<IEnumerable<Order>>(userOrders ?? new List<Order>());
    }
}

