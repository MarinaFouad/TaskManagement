# Task Management API

A production-ready RESTful API built with **ASP.NET Core 8**, following DDD-style layered architecture. Features JWT authentication with refresh tokens, Redis caching, background task processing, Swagger UI with JWT support, and clean global exception handling.

---

## Table of Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Setup & Run](#setup--run)
- [Seeded Admin Credentials](#seeded-admin-credentials)
- [API Reference](#api-reference)
- [Key Design Decisions](#key-design-decisions)
- [Business Logic](#business-logic)
- [Redis Caching](#redis-caching)
- [Background Processing](#background-processing)
- [Authentication Flow](#authentication-flow)
- [Assumptions](#assumptions)

---

## Architecture

```
┌─────────────────────────────────────────┐
│           TaskManagement.API            │  ← Controllers, Middleware, DI setup
├─────────────────────────────────────────┤
│       TaskManagement.Application        │  ← Services, DTOs, Interfaces, Background
├─────────────────────────────────────────┤
│        TaskManagement.Domain            │  ← Entities, Enums, Exceptions
├─────────────────────────────────────────┤
│      TaskManagement.Infrastructure      │  ← EF Core, Redis, JWT, BCrypt, UoW
└─────────────────────────────────────────┘
```

Dependencies flow inward: API → Application → Domain ← Infrastructure → Application (via interfaces).

---

## Tech Stack

| Concern               | Technology                            |
|-----------------------|---------------------------------------|
| Framework             | ASP.NET Core 8 Web API                |
| ORM                   | Entity Framework Core 8 + SQL Server  |
| Authentication        | JWT Bearer + Refresh Tokens           |
| Password Hashing      | BCrypt.Net (work factor 12)           |
| Caching               | Redis via StackExchange.Redis         |
| Background Processing | .NET `BackgroundService` (hosted)     |
| Documentation         | Swashbuckle / Swagger UI + JWT        |
| Logging               | Serilog (console + structured)        |

---

## Project Structure

```
TaskManagement/
├── TaskManagement.sln
└── src/
    ├── TaskManagement.API/
    │   ├── Controllers/
    │   │   ├── AuthController.cs       # Register, Login, RefreshToken, Revoke
    │   │   ├── UsersController.cs      # Profile + Admin user management
    │   │   └── TasksController.cs      # CRUD + status update
    │   ├── Extensions/
    │   │   └── ServiceExtensions.cs    # DI wiring, JWT config, Swagger
    │   ├── Middleware/
    │   │   └── GlobalExceptionMiddleware.cs
    │   ├── appsettings.json
    │   └── Program.cs
    │
    ├── TaskManagement.Application/
    │   ├── BackgroundServices/
    │   │   └── TaskProcessorBackgroundService.cs
    │   ├── DTOs/
    │   │   ├── ApiResponse.cs          # Generic wrapper
    │   │   ├── AuthDtos.cs
    │   │   └── TaskDtos.cs
    │   ├── Interfaces/
    │   │   ├── IRepositories.cs
    │   │   └── IServices.cs
    │   └── Services/
    │       ├── AuthService.cs
    │       ├── UserService.cs
    │       ├── TaskService.cs
    │       └── InMemoryTaskQueue.cs
    │
    ├── TaskManagement.Domain/
    │   ├── Entities/
    │   │   ├── User.cs
    │   │   ├── UserTask.cs
    │   │   └── RefreshToken.cs
    │   ├── Enums/
    │   │   ├── UserRole.cs
    │   │   ├── TaskStatus.cs
    │   │   └── TaskPriority.cs
    │   └── Exceptions/
    │       └── DomainExceptions.cs
    │
    └── TaskManagement.Infrastructure/
        ├── Caching/
        │   └── RedisCacheService.cs
        ├── Data/
        │   ├── AppDbContext.cs
        │   ├── DbSeeder.cs
        │   ├── UnitOfWork.cs
        │   ├── Migrations/
        │   └── Repositories/
        │       ├── UserRepository.cs
        │       ├── TaskRepository.cs
        │       └── RefreshTokenRepository.cs
        └── Services/
            ├── JwtTokenService.cs
            └── BcryptPasswordService.cs
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (local or Docker)
- [Redis](https://redis.io/) (local or Docker)

**Quick Docker setup for dependencies:**
```bash
# SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name sql -d mcr.microsoft.com/mssql/server:2022-latest

# Redis
docker run -p 6379:6379 --name redis -d redis:7-alpine
```

---

## Setup & Run

### 1. Clone and configure

```bash
git clone <repository-url>
cd TaskManagement
```

Update `src/TaskManagement.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "TaskManagementAPI",
    "Audience": "TaskManagementClients"
  }
}
```

> ⚠️ **Production**: Store secrets in environment variables or Azure Key Vault — never in appsettings.

### 2. Apply database migrations

```bash
cd src/TaskManagement.API
dotnet ef database update --project ../TaskManagement.Infrastructure
```

Or manually run the migration SQL against your SQL Server instance.

### 3. Run the API

```bash
cd src/TaskManagement.API
dotnet run
```

Swagger UI opens at: **http://localhost:5000** (or https://localhost:5001)

---

## Seeded Admin Credentials

| Field    | Value               |
|----------|---------------------|
| Email    | `admin@example.com` |
| Password | `Admin@123`         |
| Role     | `Admin`             |

The admin user is created automatically on first startup via `DbSeeder.SeedAsync()`.

---

## API Reference

### Authentication

| Method | Endpoint                  | Auth     | Description                    |
|--------|---------------------------|----------|--------------------------------|
| POST   | `/api/auth/register`      | None     | Register a new user            |
| POST   | `/api/auth/login`         | None     | Login and receive tokens       |
| POST   | `/api/auth/refresh-token` | None     | Refresh access token           |
| POST   | `/api/auth/revoke-token`  | Required | Revoke refresh token (logout)  |

### Users

| Method | Endpoint          | Auth           | Description                   |
|--------|-------------------|----------------|-------------------------------|
| GET    | `/api/users/me`   | Required       | Get current user profile      |
| GET    | `/api/users`      | Admin only     | List all users                |
| POST   | `/api/users`      | Admin only     | Create a new user             |
| DELETE | `/api/users/{id}` | Admin only     | Soft-delete a user            |

### Tasks

| Method | Endpoint                    | Auth     | Description                             |
|--------|-----------------------------|----------|-----------------------------------------|
| POST   | `/api/tasks`                | Required | Create a task (queued for processing)   |
| GET    | `/api/tasks`                | Required | Get all own tasks (sorted by priority)  |
| GET    | `/api/tasks/{id}`           | Required | Get task by ID (Redis cached)           |
| PATCH  | `/api/tasks/{id}/status`    | Required | Update task status (invalidates cache)  |

### Using Swagger

1. Open Swagger UI at the root URL
2. Call `POST /api/auth/login` with admin or user credentials
3. Copy the `accessToken` from the response
4. Click **Authorize** (top right), paste the token
5. All protected endpoints are now accessible

---

## Key Design Decisions

### Repository + Unit of Work
All data access goes through `IUnitOfWork`, keeping services infrastructure-agnostic. This makes unit testing straightforward — swap the real UoW for a mock.

### Global Soft-Delete Filter
`AppDbContext` applies `HasQueryFilter(u => !u.IsDeleted)` on `User`, so deleted users are automatically excluded from all queries without any code changes in repositories or services.

### Enum Stored as String
`Status`, `Priority`, and `Role` are stored as strings in the DB (not integers). This makes the database self-documenting and avoids broken reads when enum values are reordered.

### Cache Resilience
`RedisCacheService` wraps all Redis operations in try/catch. If Redis is down, the API degrades gracefully and hits the database — it never throws to the user.

### BCrypt Work Factor 12
Slightly above the default of 10 for better brute-force resistance. Adjust in `BcryptPasswordService` based on your hardware benchmarks.

---

## Business Logic

Two key business rules in `TaskService`:

**1. No duplicate task titles per user per day**
```
POST /api/tasks with { "title": "Deploy release" }
→ If the same user already has "Deploy release" created today → 409 Conflict
```

**2. Tasks sorted by priority, then creation date**
```
GET /api/tasks
→ Critical tasks first → High → Medium → Low
→ Within same priority: oldest first (FIFO)
```

---

## Redis Caching

`GET /api/tasks/{id}` uses a cache-aside pattern:

```
Request → Check Redis (key: "task:{id}")
              ↓ Miss                ↓ Hit
         Load from DB          Return cached DTO
         Store in Redis
         (TTL: 5 minutes)
         Return DTO
```

Cache is invalidated on:
- `PATCH /api/tasks/{id}/status` — explicit `cache.RemoveAsync`
- Background processor marking task as `IsProcessed = true` — also invalidates

---

## Background Processing

When a task is created:

1. Task is persisted to SQL Server
2. Task ID is enqueued into `InMemoryTaskQueue` (a `ConcurrentQueue<Guid>`)
3. `TaskProcessorBackgroundService` (runs as a hosted service) dequeues IDs
4. Simulates processing (500ms delay)
5. Marks `IsProcessed = true` and updates `UpdatedAt`
6. Invalidates Redis cache for that task

The queue is a singleton shared between the web pipeline and the background service, which is safe because `ConcurrentQueue` is thread-safe.

> For production at scale, replace `InMemoryTaskQueue` with RabbitMQ, Azure Service Bus, or Hangfire. The interface (`ITaskQueue`) makes this a drop-in replacement.

---

## Authentication Flow

```
Register/Login
    ↓
Server returns:
  - accessToken (JWT, 1hr expiry)
  - refreshToken (opaque, 7-day expiry, stored in DB)

Access protected endpoint:
  Authorization: Bearer <accessToken>

Token expired:
  POST /api/auth/refresh-token { refreshToken }
  → New accessToken + new refreshToken (rotation)
  → Old refreshToken revoked in DB

Logout:
  POST /api/auth/revoke-token { refreshToken }
  → Token marked IsRevoked = true in DB
```

---

## Assumptions

1. **Single-tenant**: No multi-tenancy — each user sees only their own tasks.
2. **No task deletion**: The spec didn't require task deletion, so it's omitted.
3. **Admin sees all tasks**: Not implemented — admin manages users, not tasks. Can be added easily.
4. **Redis required**: The app will start without Redis but `GET /api/tasks/{id}` will always hit the DB (graceful degradation).
5. **In-memory queue**: Restarts lose unprocessed queue items. For production, use a durable message broker.
6. **No email verification**: Registration is immediate without email confirmation.
7. **JWT secret minimum length**: Must be at least 32 characters. Validated at startup via `SymmetricSecurityKey`.
