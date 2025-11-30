using UserService.Data;
using UserService.Protos;
using Grpc.Core;

namespace UserService.Services;

public class UserGrpcService : Protos.UserService.UserServiceBase
{
    private readonly IUserRepository _repository;

    public UserGrpcService(IUserRepository repository)
    {
        _repository = repository;
    }

    public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var user = await _repository.GetByIdAsync(request.Id);
        
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User with id {request.Id} not found"));
        }

        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
    }

    public override async Task<GetAllUsersResponse> GetAllUsers(GetAllUsersRequest request, ServerCallContext context)
    {
        var users = await _repository.GetAllAsync();
        
        var response = new GetAllUsersResponse();
        response.Users.AddRange(users.Select(u => new UserResponse
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email
        }));

        return response;
    }
}

