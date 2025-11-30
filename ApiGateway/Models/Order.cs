namespace ApiGateway.Models;

public record Order(int Id, int UserId, int ProductId, int Quantity, decimal TotalPrice, DateTime OrderDate);

