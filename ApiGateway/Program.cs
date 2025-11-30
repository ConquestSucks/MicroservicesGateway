using ApiGateway.Configuration;
using ApiGateway.Endpoints;
using ApiGateway.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddRedis(builder.Configuration)
    .AddHttpClientsWithPolly(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UsePrometheusMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RateLimitingMiddleware>();

app.MapProfileEndpoints();
app.MapAuthEndpoints(builder.Configuration);

app.Run();
