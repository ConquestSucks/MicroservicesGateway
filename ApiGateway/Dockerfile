FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["MicroservicesGateway.sln", "./"]
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
COPY ["UserService/UserService.csproj", "UserService/"]
COPY ["OrderService/OrderService.csproj", "OrderService/"]
COPY ["ProductService/ProductService.csproj", "ProductService/"]

RUN dotnet restore "MicroservicesGateway.sln"

COPY ApiGateway/ ./ApiGateway/

COPY UserService/Models/ ./UserService/Models/
COPY UserService/Data/ ./UserService/Data/
COPY UserService/Endpoints/ ./UserService/Endpoints/
COPY UserService/Configuration/ ./UserService/Configuration/
COPY UserService/Protos/ ./UserService/Protos/
COPY UserService/Services/ ./UserService/Services/
COPY UserService/Program.cs ./UserService/

COPY OrderService/Models/ ./OrderService/Models/
COPY OrderService/Data/ ./OrderService/Data/
COPY OrderService/Endpoints/ ./OrderService/Endpoints/
COPY OrderService/Configuration/ ./OrderService/Configuration/
COPY OrderService/Protos/ ./OrderService/Protos/
COPY OrderService/Services/ ./OrderService/Services/
COPY OrderService/Program.cs ./OrderService/

COPY ProductService/Models/ ./ProductService/Models/
COPY ProductService/Data/ ./ProductService/Data/
COPY ProductService/Endpoints/ ./ProductService/Endpoints/
COPY ProductService/Configuration/ ./ProductService/Configuration/
COPY ProductService/Protos/ ./ProductService/Protos/
COPY ProductService/Services/ ./ProductService/Services/
COPY ProductService/Program.cs ./ProductService/

WORKDIR "/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/ApiGateway"
RUN rm -f ../UserService/appsettings*.json ../OrderService/appsettings*.json ../ProductService/appsettings*.json || true
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:CopyOutputSymbolsToPublishDirectory=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]


