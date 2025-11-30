using Microsoft.AspNetCore.Server.Kestrel.Core;
using ProductService.Configuration;
using ProductService.Data;
using ProductService.Endpoints;
using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http1);
    options.ListenAnyIP(8081, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

builder.Services.AddGrpc();

var app = builder.Build();

app.UsePrometheusMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapProductEndpoints();

app.MapGrpcService<ProductGrpcService>();

app.Run();
