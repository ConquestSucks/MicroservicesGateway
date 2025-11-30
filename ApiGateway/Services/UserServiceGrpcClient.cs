using ApiGateway.Models;
using Grpc.Net.Client;
using Serilog;
using UserService.Protos;

namespace ApiGateway.Services;

public class UserServiceGrpcClient : IUserServiceClient
{
    private readonly UserService.Protos.UserService.UserServiceClient _client;
    private readonly Serilog.ILogger _logger;

    public UserServiceGrpcClient(IConfiguration configuration, Serilog.ILogger logger)
    {
        var userServiceUrl = configuration["Services:UserServiceGrpc"] ?? "http://localhost:5001";
        var channel = GrpcChannel.ForAddress(userServiceUrl);
        _client = new UserService.Protos.UserService.UserServiceClient(channel);
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        try
        {
            var request = new GetUserRequest { Id = userId };
            var response = await _client.GetUserAsync(request);
            
            return new User(
                response.Id,
                response.FirstName,
                response.LastName,
                response.Email
            );
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.Warning("User {UserId} not found via gRPC", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to fetch user {UserId} via gRPC", userId);
            return null;
        }
    }
}

