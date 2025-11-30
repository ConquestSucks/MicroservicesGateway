using UserService.Models;

namespace UserService.Data;

public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<int, User> _users = new()
    {
        { 1, new User(1, "Иван", "Иванов", "ivan@example.com") },
        { 2, new User(2, "Мария", "Петрова", "maria@example.com") },
        { 3, new User(3, "Петр", "Сидоров", "petr@example.com") }
    };

    public Task<User?> GetByIdAsync(int id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<IEnumerable<User>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<User>>(_users.Values);
    }
}

