namespace ApiGateway.Models;

public record UserProfile
{
    public User? User { get; set; }
    public List<OrderProfileItem> Orders { get; set; } = new();
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

public class OrderProfileItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }
}

