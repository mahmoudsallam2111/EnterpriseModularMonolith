# Enterprise Modular Monolith — .NET 10

A production-grade Modular Monolith template built with .NET 10, Domain-Driven Design, CQRS, and strict architectural boundaries. Designed to be a real, reusable starting point for enterprise business systems — and to convert cleanly into microservices later if needed.

The template ships with three business modules — **Customers**, **Orders**, and **Users** — so you can see every cross-cutting concern in action against a realistic shape.

---

## Why a modular monolith?

A modular monolith gives you most of the benefits of microservices (clear bounded contexts, independent module ownership, deployable units later) without paying the operational cost up front. You can ship a single binary on day one and split modules out into services only when you have a real reason to.

The architecture follows the same patterns popularised by Kamil Grzybek's Modular Monolith with DDD reference, ABP Framework, and eShopOnContainers — adapted to modern .NET 10 idioms (Minimal APIs, OpenTelemetry, central package management, generated regex, primary constructors, `[]` collection expressions).

---

## High-level architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                       EnterpriseModularMonolith.Api                       │
│                       (single ASP.NET Core host)                          │
│                                                                           │
│   Serilog · OpenTelemetry · JWT · ProblemDetails · Health · Swagger       │
│                                                                           │
│   for each module: AddServices() → MapEndpoints() → Migrate + seed        │
└──────────────────────────────────────────────────────────────────────────┘
                  ↓                       ↓                       ↓
   ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐
   │  Users module      │  │  Customers module  │  │  Orders module     │
   │  ───────────────   │  │  ───────────────   │  │  ───────────────   │
   │  Domain            │  │  Domain            │  │  Domain            │
   │  Application       │  │  Application       │  │  Application       │
   │  Infrastructure    │  │  Infrastructure    │  │  Infrastructure    │
   │  Presentation      │  │  Presentation      │  │  Presentation      │
   │  Contracts         │  │  Contracts         │  │  Contracts         │
   │  IntegrationEvents │  │  IntegrationEvents │  │  IntegrationEvents │
   │                    │  │                    │  │                    │
   │  schema: users     │  │  schema: customers │  │  schema: orders    │
   └────────────────────┘  └────────────────────┘  └────────────────────┘
                  ↓                       ↓                       ↓
   ┌──────────────────────────────────────────────────────────────────────┐
   │                          BuildingBlocks                              │
   │  SharedKernel · Domain · Application · EventBus · UnitOfWork ·       │
   │  Infrastructure · Presentation · Observability · MultiTenancy        │
   └──────────────────────────────────────────────────────────────────────┘
```

Modules share a single PostgreSQL database but each owns its own schema (`customers`, `orders`, `users`). Migrations are scoped per module — no module's migration ever touches another module's tables.

---

## Dependency rules

Strict, enforced by NetArchTest at build time (see `tests/ArchitectureTests`).

| Layer | May depend on |
|---|---|
| **Domain** (per module) | BuildingBlocks.Domain, BuildingBlocks.SharedKernel — nothing else. No EF, no AspNetCore, no Serilog. |
| **Application** (per module) | Its own Domain + BuildingBlocks.Application/EventBus + **other modules' Contracts and IntegrationEvents only**. No other module's Domain or Infrastructure. |
| **Infrastructure** (per module) | Its own Domain + Application + BuildingBlocks.Infrastructure + EF Core + Postgres. |
| **Presentation** (per module) | Its own Application + BuildingBlocks.Presentation. |
| **Contracts** (per module) | Nothing. Pure interfaces and DTOs. |
| **IntegrationEvents** (per module) | BuildingBlocks.EventBus only. |
| **Bootstrapper** | Everything — the only project allowed to know about all modules. |

Inverted dependencies (Domain owns the repository **interface**, Infrastructure provides the **implementation**) keep the dependency arrows pointing inward.

---

## Module communication

Modules **never** call each other's domain types or DbContexts directly. They communicate only through:

1. **Public Contracts** (`*.Contracts`) — synchronous read-only lookups via an interface. E.g. Orders asks Customers "is this customer active?" via `ICustomersApi`, never by loading the `Customer` aggregate.
2. **Integration Events** (`*.IntegrationEvents`) — async, post-commit, transactional outbox-backed. E.g. when a customer is deactivated, Customers publishes `CustomerDeactivatedIntegrationEvent`; Orders subscribes and cancels open orders.
3. **Permissions** — Users module is the authority via `IPermissionService`; every other module's CQRS handlers gate themselves with `[RequiresPermission("…")]`.

```
Customer changes email                         Outbox table             Other modules
─────────────────────                          ────────────             ─────────────
ChangeEmailCommand handler                  ┌─────────────────┐
  └─ Customer.ChangeEmail()  ──┐            │ id  │ payload   │       (poller drains
       (raises domain event)   │            │     │           │        outbox after
                               │  same txn  │     │           │        commit)
  └─ enqueue integration  ─────┼──────────► │ ... │ {evt-json}│  ───►  IIntegrationEvent
       event via              │             └─────────────────┘        Handler in
       IIntegrationEventQueue │                                        another module
                               │
  └─ DbContext.SaveChangesAsync (dispatches
        domain events in-tx, persists outbox
        rows, commits)
```

A single transaction either persists the aggregate change **and** the outbox row, or neither. The outbox poller publishes after commit — at-least-once delivery without a 2PC.

---

## DDD tactical patterns in use

- **Aggregate roots** — `Customer`, `Order`, `User`, `Role`. Sealed classes with private setters, all state changes through methods.
- **Strongly typed ids** — `CustomerId`, `OrderId`, `UserId`, `RoleId`, `OrderLineId`. Records over `Guid` — accidentally passing the wrong id won't compile.
- **Value objects** — `Email`, `PersonName`, `Address`, `Money`, `UserEmail`, `PasswordHash`. Structural equality, immutable, constructed through factory methods that enforce invariants.
- **Business rules** — `IBusinessRule` with `Code` + `Message`. Aggregates `CheckRule()` to enforce invariants and throw `BusinessRuleValidationException` on violation, which the presentation layer translates to a 409 ProblemDetails.
- **Domain events** — raised inside aggregates, dispatched **before commit** via MediatR.
- **Integration events** — published **after commit**, via the outbox.
- **Specifications** — `Specification<T>` composable filter + include + paging objects, used by `EfReadRepository<TDb,T>`.
- **Domain services** — only when behaviour doesn't fit on an aggregate. The template uses very few; most behaviour lives on the aggregate where it belongs.

---

## CQRS & the request pipeline

Every command/query goes through MediatR. The pipeline is wired by `BuildingBlocks.Application.DependencyInjection.AddApplicationPipeline`:

```
HTTP request
   ↓
Minimal API endpoint  (translates body/path → command/query)
   ↓
MediatR pipeline behaviors  (outermost first):
   1. LoggingBehavior          — structured log + elapsed ms
   2. TracingBehavior          — OpenTelemetry activity per handler
   3. ValidationBehavior       — FluentValidation; failure → Result.Failure
   4. AuthorizationBehavior    — [RequiresPermission] enforcement
   5. UnitOfWorkBehavior       — begins ambient UoW (commands only)
   ↓
Handler (returns Result<T>)
   ↓
ResultExtensions.ToHttpResult  → 200 / 201 / 4xx ProblemDetails
```

Queries skip the UnitOfWork behavior — they're read-only and use `AsNoTracking()` projections.

---

## Ambient Unit of Work

Inspired by ABP's `IUnitOfWorkManager`. An AsyncLocal-backed `AmbientUnitOfWorkAccessor` holds a stack of nested UoWs:

```csharp
await using var uow = _uowManager.Begin();
await _repo.AddAsync(aggregate, ct);
await uow.CompleteAsync(ct);
```

- Nested `Begin()` calls reuse the outer UoW (unless `RequiresNew`).
- `CompleteAsync()` runs `SaveChanges` + `Commit` + post-commit callbacks (used to publish integration events after the transaction succeeds).
- Disposing without `CompleteAsync` rolls back.
- The MediatR `UnitOfWorkBehavior` wraps every command automatically — handlers don't need to know.

---

## Database & EF Core

- **One DbContext per module** — `CustomersDbContext`, `OrdersDbContext`, `UsersDbContext` — each in its own schema.
- **Base `ModuleDbContext`** — handles domain-event dispatch in `SaveChangesAsync` (events fire pre-commit, inside the transaction).
- **Interceptors** registered per-DbContext:
  - `AuditingInterceptor` — stamps `CreatedAt/By`, `UpdatedAt/By` for any `IAuditableEntity`.
  - `SoftDeleteInterceptor` — turns `EntityState.Deleted` into a flag flip; query filters hide soft-deleted rows.
  - `OutboxInterceptor` — drains the per-module `OutboxAccumulator` into the `outbox_messages` table in the same transaction as the aggregate change.
- **Concurrency** — every aggregate maps a Postgres `xmin` column as an `IsConcurrencyToken()`.
- **Resiliency** — `EnableRetryOnFailure(3)` on the Npgsql provider.
- **Snake-case naming** — via `EFCore.NamingConventions`.

Migrations are isolated:

```bash
dotnet ef migrations add Initial \
  --project src/Modules/Customers/Customers.Infrastructure \
  --startup-project src/Bootstrapper/EnterpriseModularMonolith.Api \
  --context CustomersDbContext --output-dir Persistence/Migrations
```

---

## Observability

- **Serilog** — structured logs to console + Seq; correlation id flowed through `LogContext` from `CorrelationIdMiddleware`.
- **OpenTelemetry** — traces and metrics with AspNetCore / HttpClient / EF Core / Runtime instrumentation; OTLP-exported (Jaeger by default at `http://localhost:4317`).
- **`X-Correlation-Id`** — read or generated per request, echoed in the response, propagated into every log line and ProblemDetails response.
- **Health checks** — `/health` returns the status of every module's DbContext.

---

## Security

- **JWT bearer authentication** with options in `appsettings.json` under `"Jwt"`.
- **`ITokenIssuer`** — the Users module issues tokens with `sub`, `unique_name`, `email`, `permission` claims.
- **`ICurrentUser`** — application-layer abstraction over the principal; modules never reference `HttpContext` or `ClaimsPrincipal`.
- **`IPermissionService`** — Users module is the authority; cached per-user permission lookup, consumed by `AuthorizationBehavior`.
- **`[RequiresPermission("customers.manage")]`** — declarative permission requirements on commands and queries.
- **BCrypt** password hashing through the `IPasswordHasher` abstraction.

---

## Enterprise features

Every concern requested by the spec is wired up through a thin abstraction in BuildingBlocks.Application with a default in BuildingBlocks.Infrastructure — swap implementations without touching modules.

| Concern | Interface | Default implementation |
|---|---|---|
| Clock | `IClock` | `SystemClock` |
| Caching | `ICacheService` | `InMemoryCacheService` (swap for Redis later) |
| Feature flags | `IFeatureFlags` | wraps Microsoft.FeatureManagement |
| Distributed lock | `IDistributedLock` | `InMemoryDistributedLock` (swap for RedLock) |
| Background jobs | `IBackgroundJobScheduler` | abstraction; plug in Quartz/Hangfire |
| Audit logging | `IAuditLogger` | `LoggerAuditLogger` (Serilog → Seq) |
| Multi-tenancy | `ITenantContext` | `NullTenantContext` — single-tenant default; flip to header/host resolver when needed |
| Event bus | `IIntegrationEventBus` | `InProcessIntegrationEventBus`; outbox drain converts to a broker (RabbitMQ/Azure Service Bus) when you split the monolith |

---

## Project layout

```
EnterpriseModularMonolith/
├── docker-compose.yml          # postgres 17, seq, jaeger, redis
├── Directory.Build.props       # net10.0, nullable, warnings-as-errors
├── Directory.Packages.props    # central package management
├── global.json                 # .NET 10 SDK pin
├── EnterpriseModularMonolith.sln
│
├── src/
│   ├── BuildingBlocks/
│   │   ├── BuildingBlocks.SharedKernel/    Result, Error, PagedList, IClock
│   │   ├── BuildingBlocks.Domain/          Entity, Aggregate, ValueObject, StronglyTypedId, Specification, IDomainEvent, IBusinessRule, IAuditableEntity, ISoftDeletable, DomainException
│   │   ├── BuildingBlocks.Application/     ICommand/IQuery, MediatR behaviors, ICurrentUser, IPermissionService, RequiresPermission, IUnitOfWork, IModule, ICacheService, IFeatureFlags, IBackgroundJobScheduler, IDistributedLock, IAuditLogger
│   │   ├── BuildingBlocks.EventBus/        IIntegrationEvent(Bus|Queue|Handler), IDomainEventDispatcher, Outbox/Inbox messages, InProcess implementations
│   │   ├── BuildingBlocks.UnitOfWork/      AmbientUnitOfWorkAccessor, UnitOfWorkManager, ChildUnitOfWork
│   │   ├── BuildingBlocks.Infrastructure/  ModuleDbContext, interceptors, EF repositories, OutboxProcessor, EfCoreUnitOfWork, default cache/lock/audit/feature-flag implementations, IDataSeeder
│   │   ├── BuildingBlocks.Presentation/    IModuleEndpoints, ResultExtensions, CorrelationIdMiddleware, GlobalExceptionMiddleware
│   │   ├── BuildingBlocks.Observability/   Serilog + OpenTelemetry wiring
│   │   └── BuildingBlocks.MultiTenancy/    ITenantContext, NullTenantContext
│   │
│   ├── Modules/
│   │   ├── Customers/{Contracts,IntegrationEvents,Domain,Application,Infrastructure,Presentation}
│   │   ├── Orders/   {same six}
│   │   └── Users/    {same six}
│   │
│   └── Bootstrapper/
│       └── EnterpriseModularMonolith.Api/  Program, Composition/{ModuleRegistry,Authentication,Infrastructure,Endpoints,MigrationsAndSeed,HttpContextCurrentUser}
│
└── tests/
    ├── ArchitectureTests/         NetArchTest — module isolation, sealed aggregates, no AspNetCore in Application, no EF in Domain, Contracts have no dependencies
    ├── Customers.UnitTests/       Domain behaviour (Email normalisation, rule enforcement, events raised)
    ├── Orders.UnitTests/          Order state machine, Money arithmetic
    ├── Users.UnitTests/           Lockout after N failed attempts
    └── IntegrationTests/          WebApplicationFactory + Testcontainers PostgreSQL, end-to-end smoke
```

---

## Running it locally

Prerequisites:
- .NET 10 SDK
- Docker (for Postgres + Seq + Jaeger)

```bash
# Start dependencies
docker compose up -d

# Apply migrations + seed (the host does this on start when Migrations:RunOnStartup=true)
dotnet run --project src/Bootstrapper/EnterpriseModularMonolith.Api

# Browse
#   Swagger:  http://localhost:5000/swagger
#   Health:   http://localhost:5000/health
#   Seq:      http://localhost:5341
#   Jaeger:   http://localhost:16686
```

Seeded credentials: `admin / Admin#12345`.

```bash
# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userNameOrEmail":"admin","password":"Admin#12345"}'
```

---

## Adding a new module

1. Copy `src/Modules/Customers/` to `src/Modules/{NewModule}/` and rename the six projects.
2. Implement your aggregate, value objects, business rules, and domain events in `*.Domain`.
3. Write commands/queries/handlers/validators in `*.Application`.
4. Wire EF mappings, repositories, and an `IModule` implementation in `*.Infrastructure`.
5. Map Minimal API endpoints in `*.Presentation`.
6. Add **one line** to `src/Bootstrapper/.../Composition/ModuleRegistry.cs` and **one line** to `EndpointsBootstrap.cs`.
7. Add the projects to `EnterpriseModularMonolith.sln`.

Architecture tests will fail-fast if you accidentally reference another module's internals.

---

## Tradeoffs and design decisions

- **Single database, schema-per-module.** Easier ops than database-per-module, still gives logical isolation and makes the future split-out manageable. You **don't** join across schemas in queries — use the public API contract or integration events.
- **In-process integration event bus by default.** Backed by a real Outbox so when you move to a broker, the publish-side guarantees don't change. Microservice migration = swap the `IIntegrationEventBus` implementation and run the outbox processor as a sidecar that pushes to the broker.
- **Read repositories project directly in handlers.** This keeps queries simple and discoverable. For very large reads you'd add denormalised projection tables — the `ICustomerReadModel` / `IOrderReadModel` interfaces are the seam to swap in a separate read store.
- **AsyncLocal Unit of Work** is convenient but ties commits to "wherever the using-block ends." For long-running orchestrations (sagas), use explicit transactions or rely on the outbox + idempotent consumers.
- **Domain events are dispatched in-transaction.** That means a failing handler will roll the whole UoW back. Keep domain event handlers small and side-effect-free; for fire-and-forget integrations, raise an integration event instead.
- **TreatWarningsAsErrors=true.** Strict, but it catches many real bugs before they ship. Per-rule severities can be tuned in `.editorconfig`.

---

## Scalability considerations

- **Vertical first.** Postgres handles a lot of concurrent traffic at a single node; the monolith handles a lot of RPS on a few replicas. Don't split prematurely.
- **Horizontal next.** The host is stateless — run multiple replicas behind a load balancer. Move the cache and distributed lock to Redis when you do this (drop-in: re-register `ICacheService` and `IDistributedLock`).
- **Read scaling.** Add Postgres read replicas and switch the read-model EF contexts to a read connection string.
- **Module splitting.** When a module's deployment cadence or data volume diverges from the rest, lift it out: bring its six projects into a new repo, change the host to be its own ASP.NET Core service, swap the event bus to a real broker, repoint the `*.Contracts` consumers at an HTTP/gRPC client implementing the same interface.

---

## Path to microservices

Because the boundaries are already enforced today, the lift is mostly mechanical:

1. **Stand up a broker** (RabbitMQ, Azure Service Bus). Implement `IIntegrationEventBus` on top of it. The Outbox publishes there; consumers run inboxes with `InboxMessage` for idempotency. **Application code does not change.**
2. **Replace cross-module Contracts calls with HTTP/gRPC clients** that implement the same `ICustomersApi`/`IOrdersApi`/`IUsersApi` interfaces.
3. **Lift a module's six projects into a new solution and new host.** Wire `AddPlatform()`, the module's `AddServices`, OpenTelemetry, Serilog, etc. — the host bootstrapper is small.
4. **Cut the connection string over** to a per-service database. The schema already lives on its own — point Npgsql at the new database and run the existing migrations there.
5. **Decommission the in-process registration** of the old module from the monolith's `ModuleRegistry`.

The architectural tests in this repo are the contract that makes this lift safe — they fail-fast at PR time if anyone reaches across a module boundary.

---

## License

This template is provided as a starting point — copy it, adapt it, ship it.
