# Mockups (C#/.NET)

## 1) Назначение проекта
Веб‑приложение на ASP.NET Core MVC для управления меню, корзиной и заказами с пользовательской регистрацией/аутентификацией. Сценарии включают просмотр меню, добавление блюд в корзину, оформление заказа и администрирование заказов/меню.

## 2) Архитектура
**Слои и компоненты:**
- **Controllers** (`Application/Mockups/Controllers`) — MVC‑контроллеры и маршруты.
- **Services** (`Application/Mockups/Services`) — бизнес‑логика (пользователи, адреса, меню, корзина, заказы).
- **Repositories** (`Application/Mockups/Repositories`) — доступ к данным (меню, адреса, заказы).
- **Storage** (`Application/Mockups/Storage`) — сущности и `ApplicationDbContext` (EF Core + Identity).
- **Hosted Service** — `CartsCleaner` очищает старые корзины по расписанию.

**Схема данных (основные сущности):**
- `User` (Identity): `Id`, `Name`, `BirthDate`, `Email`, `Phone`.
- `Role`, `UserRole` (Identity).
- `Address`: адрес пользователя (`StreetName`, `HouseNumber`, `EntranceNumber`, `FlatNumber`, `Name`, `IsMainAddress`, `UserId`).
- `MenuItem`: блюдо (`Name`, `Price`, `Description`, `Category`, `IsVegan`, `PhotoPath`, `IsDeleted`).
- `Order`: заказ (`CreationTime`, `DeliveryTime`, `Cost`, `Discount`, `Address`, `Status`, `UserId`).
- `OrderMenuItem`: связь заказ‑блюдо (`OrderId`, `ItemId`, `Amount`).
- `Cart` и `CartMenuItem` — корзина хранится в памяти (через `CartsRepository`).

**База данных и зависимости:**
- EF Core + ASP.NET Core Identity.
- Провайдер БД — **SQL Server** (`UseSqlServer` в `Program.cs`).
- Подключение задается строкой `ConnectionStrings:DefaultConnection`.

## 3) Запуск проекта

### Локально
1. Перейти в проект:
   ```bash
   cd csharp-app/Application/Mockups
   ```
2. Настроить строку подключения к SQL Server в `appsettings.json` или через переменные окружения.
3. Запуск:
   ```bash
   dotnet run
   ```
4. Открыть в браузере:
   - `https://localhost:7146`.

> Примечание: `ApplicationDbContext` вызывает `Database.EnsureCreated()` — БД и таблицы создаются автоматически при старте.

### Docker
#### Сборка и запуск контейнера
Из директории `csharp-app/Application`:

```bash
docker build -f Mockups/Dockerfile -t mockups:local .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=mockupsdb;Username=postgres;Password=postgres" \
  -e AdminCreds__Email="admin@example.com" \
  -e AdminCreds__Password="ChangeMe123!" \
  mockups:local
```

#### Локальный запуск через Docker Compose (app + PostgreSQL)
```bash
cd csharp-app/Application
docker compose up --build
```

Приложение будет доступно на `http://localhost:8080`.

### Переменные окружения
Все параметры можно переопределить через стандартную схему ASP.NET Core:

- `ConnectionStrings__DefaultConnection` — строка подключения к SQL Server.
- `AdminCreds__Email`, `AdminCreds__Password` — учетные данные администратора.
- `OrderTimeParams__MinDeliveryTime`, `OrderTimeParams__DeliveryTimeStep` — параметры доставки.
- `CartsCleaner__Time` — период очистки корзин (в минутах).
- `ASPNETCORE_ENVIRONMENT` — среда (`Development`, `Production`).

Пример для локального запуска:
```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=MockupsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
export ASPNETCORE_ENVIRONMENT=Development
```

## 4) Документация API (MVC эндпоинты)
Приложение использует MVC‑маршруты и возвращает HTML‑страницы.

### AccountController
- `GET /Account/Register` — форма регистрации.
- `POST /Account/Register` — регистрация пользователя.
- `GET /Account/Login` — форма входа.
- `POST /Account/Login` — вход.
- `GET /Account/Logout` — выход.
- `GET /Account` — профиль пользователя (требует авторизации).
- `GET /Account/AddAddress` — форма добавления адреса.
- `POST /Account/AddAddress` — добавление адреса.
- `GET /Account/EditAddress?id={addressId}` — форма редактирования адреса.
- `POST /Account/EditAddress` — сохранение адреса.
- `GET /Account/DeleteAddress?id={addressId}` — подтверждение удаления адреса.
- `POST /Account/DeleteAddress` — удаление адреса.
- `GET /Account/Edit` — форма редактирования профиля.
- `POST /Account/Edit` — сохранение профиля.

### MenuController
- `GET /Menu` — список меню.
  - Параметры фильтрации: `filterCategory` (массив), `filterIsVegan` (bool).
- `GET /Menu/Create` — создание блюда (роль `Administrator`).
- `POST /Menu/Create` — создание блюда.
- `GET /Menu/Delete?id={itemId}` — подтверждение удаления блюда (роль `Administrator`).
- `POST /Menu/Delete?id={itemId}` — удаление блюда.
- `GET /Menu/AddToCart?id={itemId}` — форма добавления в корзину.
- `POST /Menu/AddToCart` — добавление в корзину (поля: `id`, `amount`).

### CartController
- `GET /Cart` — корзина пользователя.
- `GET /Cart/Delete?id={itemId}` — подтверждение удаления позиции.
- `POST /Cart/Delete?id={itemId}` — удаление позиции.

### OrdersController
- `GET /Orders` — список заказов пользователя.
- `GET /Orders/Create` — форма оформления заказа.
- `POST /Orders/Create` — оформление заказа.

### OrdersManagementController (Admin)
- `GET /OrdersManagement` — список всех заказов.
- `GET /OrdersManagement/Details?id={orderId}` — детали заказа.
- `GET /OrdersManagement/Edit?id={orderId}` — редактирование заказа.
- `POST /OrdersManagement/Edit` — сохранение изменений.

### AnalyticsController (JSON)
Метрики собираются с момента старта приложения (in-memory) и основаны на реальных данных БД.

- `GET /analytics/summary` — сводная статистика по БД: пользователи, позиции меню, заказы, заказы за 7 дней, средний чек.
- `GET /analytics/usage` — топ эндпоинтов и среднее время обработки запроса.
- `GET /analytics/errors` — статистика по ошибкам (4xx/5xx).

**Пример запроса (summary):**
```http
GET /analytics/summary
```

**Пример ответа (summary):**
```json
{
  "totalUsers": 12,
  "totalMenuItems": 25,
  "totalOrders": 57,
  "ordersLast7Days": 8,
  "averageOrderCost": 18.4
}
```

**Пример запроса (usage):**
```http
GET /analytics/usage
```

**Пример ответа (usage):**
```json
{
  "startedAtUtc": "2024-06-20T09:15:31Z",
  "totalRequests": 124,
  "topEndpoints": [
    { "path": "/Menu", "count": 54, "averageDurationMs": 18.2 },
    { "path": "/Orders", "count": 20, "averageDurationMs": 42.7 }
  ]
}
```

**Пример запроса (errors):**
```http
GET /analytics/errors
```

**Пример ответа (errors):**
```json
{
  "totalErrors": 3,
  "total4xx": 2,
  "total5xx": 1,
  "statusCodeCounts": [
    { "statusCode": 404, "count": 2 },
    { "statusCode": 500, "count": 1 }
  ]
}
```

**Пример запроса (меню с фильтрацией):**
```http
GET /Menu?filterIsVegan=true&filterCategory=Drinks&filterCategory=Snacks
```

**Пример POST (добавление в корзину):**
```http
POST /Menu/AddToCart
Content-Type: application/x-www-form-urlencoded

id=1b1c8d3b-2c3b-4c0c-8bb2-91f4c7b9d2e7&amount=2
```

**Пример ответа:**
- HTML‑страницы Razor Views (MVC). Формат JSON не используется.

## 5) Запуск тестов
Из корня решения:
```bash
cd csharp-app/Application

dotnet test Mockups.sln
```

Или напрямую проект тестов:
```bash
dotnet test csharp-app/Application/Mockups.Tests/Mockups.Tests.csproj
```

## 6) CI/CD (GitHub Actions)

### Что делает пайплайн
- **build**: `dotnet restore` + `dotnet build`.
- **test**: `dotnet test` + coverage (`XPlat Code Coverage`).
- **lint**: `dotnet format --verify-no-changes`.
- **docker-build**: сборка Docker image.
- **deploy**: пуш Docker image в GHCR из `main/master` после успешных тестов.

### Секреты/переменные CI
Для публикации в GHCR используется `GITHUB_TOKEN` (встроенный секрет GitHub).

Для деплоя на сервер (pull из GHCR) нужны:
- `GHCR_USERNAME` — логин GitHub.
- `GHCR_TOKEN` — Personal Access Token с правами `read:packages`.

### Локальная проверка шагов CI
Из директории `csharp-app/Application`:
```bash
dotnet restore Mockups.sln
dotnet build Mockups.sln --configuration Release
dotnet test Mockups.sln --configuration Release --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-format
dotnet format Mockups.sln --verify-no-changes
```

### Деплой через GHCR (пример)
1. Залогиниться в GHCR:
   ```bash
   echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USERNAME" --password-stdin
   ```
2. Запустить контейнер:
   ```bash
   docker pull ghcr.io/<owner>/<repo>/mockups:latest
   docker run --rm -p 8080:8080 \
     -e ConnectionStrings__DefaultConnection="Host=<db-host>;Database=mockupsdb;Username=postgres;Password=postgres" \
     -e AdminCreds__Email="admin@example.com" \
     -e AdminCreds__Password="ChangeMe123!" \
     ghcr.io/<owner>/<repo>/mockups:latest
   ```
