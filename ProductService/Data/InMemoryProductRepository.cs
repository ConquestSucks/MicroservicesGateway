using ProductService.Models;

namespace ProductService.Data;

public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<int, Product> _products = new()
    {
        { 101, new Product(101, "Ноутбук", "Мощный ноутбук для работы", 999.99m) },
        { 102, new Product(102, "Смартфон", "Современный смартфон", 899.99m) },
        { 103, new Product(103, "Планшет", "Удобный планшет", 599.99m) },
        { 104, new Product(104, "Наушники", "Беспроводные наушники", 199.99m) }
    };

    public Task<Product?> GetByIdAsync(int id)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<Product>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Product>>(_products.Values);
    }
}

