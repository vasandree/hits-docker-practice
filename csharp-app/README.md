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
   - `https://localhost:5001` или `http://localhost:5000` (в зависимости от настроек SDK).

> Примечание: `ApplicationDbContext` вызывает `Database.EnsureCreated()` — БД и таблицы создаются автоматически при старте.

### Docker
Docker‑файлы отсутствуют. Для запуска в контейнере требуется добавить `Dockerfile` и конфигурацию БД вручную.

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
