## MicroservicesGateway — API Gateway с агрегацией данных и кэшированием

API Gateway (BFF) поверх трёх микросервисов:

- `UserService` — пользователи
- `OrderService` — заказы
- `ProductService` — продукты
- `ApiGateway` — точка входа для клиента, агрегирует данные из микросервисов

Всё упаковано в Docker и поднимается одной командой. Есть автоматический скрипт `test-all.ps1`, который проверяет основную функциональность.

---

## Основные фичи

- **Агрегация данных** в `ApiGateway`:
  - `GET /api/profile/{userId}` возвращает профиль пользователя с его заказами и данными о продуктах.
- **Кэширование профиля** в Redis (StackExchange.Redis):
  - основной профиль — на 30 секунд,
  - частичный профиль (fallback) — на 15 секунд.
- **Устойчивость и отказоустойчивость** с Polly:
  - **Retry** (3 попытки с экспоненциальной задержкой),
  - **Circuit Breaker** (после серии ошибок запросы временно не отправляются),
  - fallback‑логика в `ProfileAggregationService` (частичные данные, кэш).
- **JWT‑аутентификация** в `ApiGateway`:
  - простой логин `POST /api/auth/login?username=testuser`,
  - защищённый профиль `GET /api/profile/{userId}` (Bearer токен).
- **Rate limiting** (100 запросов в минуту на IP):
  - реализовано простым middleware `RateLimitingMiddleware`.
- **gRPC‑транспорт для внутренних вызовов**:
  - `ApiGateway` ходит к микросервисам через gRPC‑клиентов (`*GrpcClient`),
  - микросервисы поднимают gRPC‑сервисы (`*GrpcService`).
- **Наблюдаемость**:
  - логирование через Serilog,
  - метрики через `prometheus-net` (`/metrics` на каждом сервисе),
  - Prometheus + Grafana в Docker Compose.
- **Docker Compose** поднимает сразу:
  - `apigateway`, `userservice`, `orderservice`, `productservice`,
  - `redis`, `prometheus`, `grafana`.

---

## Требования

- **Windows / Linux / macOS** с установленным:
  - [Docker Desktop](https://www.docker.com/products/docker-desktop),
  - [.NET SDK 9.0](https://dotnet.microsoft.com/) (для локальной сборки).

---

## Быстрый старт

### 1. Клонирование репозитория

```bash
git clone https://github.com/ConquestSucks/MicroservicesGateway.git
```

### 2. Полная проверка через скрипт

В корне проекта есть скрипт **`test-all.ps1`**, который:

- запускает `docker-compose up -d`,
- ждёт поднятия контейнеров,
- получает JWT,
- вызывает агрегирующий эндпоинт,
- проверяет кэширование,
- тестирует fallback (останавливает и снова запускает `orderservice`),
- проверяет rate limiting (получение 429),
- читает метрики ApiGateway и микросервисов.

Запуск (PowerShell):

```powershell
.\test-all.ps1
```
### 3. Ручной запуск через Docker Compose

```powershell
docker-compose up -d --build
```

Сервисы будут доступны по адресам:

- ApiGateway: `http://localhost:5000`
- UserService: `http://localhost:5001`
- OrderService: `http://localhost:5002`
- ProductService: `http://localhost:5003`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (по умолчанию логин/пароль `admin` / `admin`)

---

## Основные эндпоинты

### ApiGateway

- `POST /api/auth/login?username=testuser`  
  Возвращает JWT‑токен:

  ```json
  { "token": "<JWT>" }
  ```

- `GET /api/profile/{userId}` (требует `Authorization: Bearer <JWT>`)

  ```json
  {
    "user": {
      "id": 1,
      "firstName": "Иван",
      "lastName": "Иванов",
      "email": "ivan@example.com"
    },
    "orders": [
      {
        "id": 1,
        "userId": 1,
        "product": {
          "id": 101,
          "name": "Ноутбук",
          "description": "Мощный ноутбук для работы",
          "price": 999.99
        },
        "quantity": 2,
        "totalPrice": 1999.99,
        "orderDate": "..." 
      }
    ],
    "totalOrders": 2,
    "totalSpent": 2899.98
  }
  ```

- `GET /metrics` — метрики для Prometheus.

### UserService

- `GET /api/users` — список пользователей.
- `GET /api/users/{id}` — один пользователь.
- `GET /metrics` — метрики.

### OrderService

- `GET /api/orders/user/{userId}` — заказы пользователя.
- `GET /api/orders/{id}` — один заказ.
- `GET /metrics` — метрики.

### ProductService

- `GET /api/products` — список продуктов.
- `GET /api/products/{id}` — один продукт.
- `GET /metrics` — метрики.

---

## gRPC‑транспорт

Внутренние вызовы между `ApiGateway` и микросервисами идут через gRPC.

- В микросервисах:
  - `UserService/Protos/user.proto` + `UserGrpcService`,
  - `OrderService/Protos/order.proto` + `OrderGrpcService`,
  - `ProductService/Protos/product.proto` + `ProductGrpcService`.
  - В `Program.cs` каждого сервиса настраивается Kestrel на порт `8080` с `Http1AndHttp2` и регистрируется `MapGrpcService<...>()`.

- В `ApiGateway`:
  - gRPC‑клиенты: `UserServiceGrpcClient`, `OrderServiceGrpcClient`, `ProductServiceGrpcClient`,
  - конфигурация адресов в `appsettings.json` и `docker-compose.yml` (`Services__*Grpc`),
  - флаг `Services:UseGrpc = true` задаёт использование gRPC‑клиентов вместо HttpClient‑клиентов.

---

## Структура решения

- `ApiGateway/`  
  - `Program.cs` — минимальный стартап.
  - `Models/` — модели (`User`, `Order`, `Product`, `UserProfile`, `OrderProfileItem`).
  - `Services/` — клиенты к микросервисам (HTTP и gRPC), `ProfileAggregationService`.
  - `Endpoints/` — эндпоинты (`ProfileEndpoints`, `AuthEndpoints`).
  - `Middleware/RateLimitingMiddleware.cs` — rate limit.
  - `Configuration/` — регистрация Redis, HttpClient+Polly, JWT, Prometheus.
  - `ApiGateway.http` — HTTP‑запросы для ручного теста.

- `UserService/`, `OrderService/`, `ProductService/`  
  - `Models/` — модели.
  - `Data/` — in‑memory репозитории.
  - `Endpoints/` — REST‑эндпоинты.
  - `Services/` — gRPC‑сервисы.
  - `Configuration/MonitoringConfiguration.cs` — Prometheus‑метрики.
  - `Program.cs` — минимальный стартап с Kestrel + gRPC.

- `prometheus/prometheus.yml` — конфиг Prometheus (scrape `apigateway`, `userservice`, `orderservice`, `productservice`).
- `docker-compose.yml` — сборка и запуск всех контейнеров.
- `test-all.ps1` — автоматический сценарий тестирования.
