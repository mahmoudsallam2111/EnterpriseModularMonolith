# Architecture Reference

Companion to the top-level README. Captures every architecture diagram, dependency rule, and design decision the brief calls for.

## 1. Solution structure (full tree)

```
EnterpriseModularMonolith/
├── docker-compose.yml
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── EnterpriseModularMonolith.sln
├── README.md
├── docs/ARCHITECTURE.md
│
├── src/
│   ├── BuildingBlocks/
│   │   ├── BuildingBlocks.SharedKernel
│   │   ├── BuildingBlocks.Domain
│   │   ├── BuildingBlocks.Application
│   │   ├── BuildingBlocks.EventBus
│   │   ├── BuildingBlocks.UnitOfWork
│   │   ├── BuildingBlocks.Infrastructure
│   │   ├── BuildingBlocks.Presentation
│   │   ├── BuildingBlocks.Observability
│   │   └── BuildingBlocks.MultiTenancy
│   │
│   ├── Modules/
│   │   ├── Customers/
│   │   │   ├── Customers.Contracts          public DTOs + ICustomersApi
│   │   │   ├── Customers.IntegrationEvents  CustomerRegistered/Deactivated
│   │   │   ├── Customers.Domain             Customer aggregate, VOs, rules, events
│   │   │   ├── Customers.Application        CQRS handlers + validators
│   │   │   ├── Customers.Infrastructure     CustomersDbContext, EF mappings, module wiring
│   │   │   └── Customers.Presentation       Minimal API endpoints
│   │   ├── Orders/   (same shape)
│   │   └── Users/    (same shape)
│   │
│   └── Bootstrapper/
│       └── EnterpriseModularMonolith.Api    composition root + Program.cs
│
└── tests/
    ├── ArchitectureTests          NetArchTest module-isolation, layering
    ├── Customers.UnitTests        Domain tests
    ├── Orders.UnitTests
    ├── Users.UnitTests
    └── IntegrationTests           Testcontainers PostgreSQL end-to-end
```

## 2. Layered view

```
              ┌────────────────────────────────────────────┐
              │      Bootstrapper (host / composition)     │  references every module
              └────────────────────────────────────────────┘
                                  ↑
        ┌────────────┬────────────┴────────────┬────────────┐
        │            │                         │            │
        │ Customers  │   Orders                │  Users     │  bounded contexts
        │            │                         │            │
        │ ┌────────────┐  ┌────────────┐  ┌────────────┐    │
        │ │Presentation│──│Presentation│──│Presentation│    │   thin HTTP edge
        │ └─────┬──────┘  └─────┬──────┘  └─────┬──────┘    │
        │ ┌─────▼──────┐  ┌─────▼──────┐  ┌─────▼──────┐    │
        │ │ Application│  │ Application│  │ Application│    │   CQRS + use cases
        │ └─────┬──────┘  └─────┬──────┘  └─────┬──────┘    │
        │ ┌─────▼──────┐  ┌─────▼──────┐  ┌─────▼──────┐    │
        │ │  Domain    │  │  Domain    │  │  Domain    │    │   aggregates + invariants
        │ └─────┬──────┘  └─────┬──────┘  └─────┬──────┘    │
        │ ┌─────▼──────┐  ┌─────▼──────┐  ┌─────▼──────┐    │
        │ │Infrastruct.│  │Infrastruct.│  │Infrastruct.│    │   EF/Postgres/JWT
        │ └────────────┘  └────────────┘  └────────────┘    │
        └────────────────────────────────────────────────────┘
                                  ↑
              ┌────────────────────────────────────────────┐
              │              BuildingBlocks                │  shared kernel
              └────────────────────────────────────────────┘
```

Dependency arrows point upward (toward more concrete code). Domain at the bottom depends on nothing module-specific — it's pure C# + BuildingBlocks.Domain.

## 3. Dependency rules (formal)

```
Allowed:                                        Forbidden:
                                                
Domain   → BuildingBlocks.Domain                Domain   → Infrastructure
Domain   → BuildingBlocks.SharedKernel          Domain   → EF Core
                                                Domain   → Microsoft.AspNetCore
Application → own Domain                        Application → Microsoft.AspNetCore
Application → BuildingBlocks.Application        Application → EF Core
Application → BuildingBlocks.EventBus           Application → another module's Domain
Application → other modules' .Contracts         Application → another module's Infrastructure
Application → other modules' .IntegrationEvents
                                                Infrastructure → another module's Domain
Infrastructure → own Domain + Application       Infrastructure → another module's Infrastructure
Infrastructure → BuildingBlocks.Infrastructure
Infrastructure → EF Core, Npgsql                Contracts → ANY non-System assembly
                                                
Presentation → own Application                  Presentation → own Domain (forbidden — go via App)
Presentation → BuildingBlocks.Presentation      Presentation → another module's anything

Bootstrapper → everything                       (no restrictions on the composition root)
```

Enforced by `tests/ArchitectureTests/ModuleIsolationTests.cs` and `CleanArchitectureTests.cs`.

## 4. Module communication flow

### 4a. Synchronous read (Orders needs Customer info)

```
Orders.Application                       Customers.Contracts          Customers.Infrastructure
─────────────────                        ────────────────────          ────────────────────────
PlaceOrderCommandHandler
  │
  └── ICustomersApi.IsCustomerActiveAsync(...)  ←──── DI resolves to ────  CustomersApi (impl)
                                                                          │
                                                                          └── EF query (NoTracking
                                                                              projection)
```

Orders has a project reference to `Customers.Contracts` (one interface + one DTO record). It has **no** reference to `Customers.Domain` or `Customers.Infrastructure`. The DI container connects the dots at runtime.

### 4b. Asynchronous event (Customer deactivated → cancel open Orders)

```
Customers.Application                  Customers.Infra (outbox)        Postgres
─────────────────────                  ──────────────────────           ────────
DeactivateCustomerCommandHandler
  │
  ├── customer.Deactivate(reason)  (raises CustomerDeactivatedDomainEvent)
  ├── queue: CustomerDeactivatedIntegrationEvent
  │
  └── UoW.CompleteAsync ──┐
                         │
                         ▼
                  SaveChangesAsync:
                    1. dispatch domain events (in-tx)
                    2. OutboxInterceptor writes the integration event row
                    3. EF.SaveChanges
                    4. tx.Commit                                       ──►  customers.customers row updated
                                                                              customers.outbox_messages row added
                                                                              (single transaction)


                                              OutboxProcessor<CustomersDbContext>
                                              ────────────────────────────────────
                                              every 5s: pick up unprocessed rows
                                                       └── IIntegrationEventBus.PublishAsync<T>(evt)

                                              ▼
                                              InProcessIntegrationEventBus
                                              ▼
Orders.Application                     ────  IIntegrationEventHandler<CustomerDeactivatedIntegrationEvent>
─────────────────                            │
CustomerDeactivatedHandler  ←─────────────────┘
  │
  └── for each open OrderId  →  mediator.Send(new CancelOrderCommand(...))
                                  (which goes through the SAME pipeline:
                                   validation → authorization → its own UoW)
```

When this monolith is later split, swap `InProcessIntegrationEventBus` for a RabbitMQ/Azure Service Bus implementation — the outbox processor publishes to the broker instead of in-process, and Orders subscribes through MassTransit/whatever. The handlers don't change.

## 5. The request pipeline (sample command)

```
POST /api/v1/orders                                 (Minimal API endpoint)
   │
   │ deserialise body → PlaceOrderCommand
   │
   ▼
ISender.Send(command)                               (MediatR)
   │
   ├── LoggingBehavior         log "Handling PlaceOrderCommand"
   │       ▼
   ├── TracingBehavior         start OTel activity "Mediator PlaceOrderCommand"
   │       ▼
   ├── ValidationBehavior      run PlaceOrderCommandValidator
   │                           on failure → Result.Failure(Validation error)
   │       ▼
   ├── AuthorizationBehavior   verify [RequiresPermission("orders.manage")]
   │                           on failure → Result.Failure(Forbidden error)
   │       ▼
   ├── UnitOfWorkBehavior      uow = manager.Begin()   ┐
   │       ▼                                            │
   │       PlaceOrderCommandHandler                     │  ambient AsyncLocal UoW —
   │         │                                          │  all repos see the same one
   │         ├── customersApi.IsCustomerActiveAsync     │
   │         ├── order = Order.Draft(...)               │
   │         ├── order.AddLine(...) etc.                │
   │         ├── order.Place(now)  →  raises domain event
   │         ├── repo.AddAsync(order)                   │
   │         └── queue.Enqueue(OrderPlacedIntegrationEvent)
   │       ▲                                            │
   │       │                                            │
   │     uow.CompleteAsync                              │
   │       │                                            │
   │       │ SaveChangesAsync:                          │
   │       │   • dispatch domain events in-tx          │
   │       │   • outbox interceptor writes integration  │
   │       │     event row alongside aggregate change   │
   │       │   • EF SaveChanges                         │
   │       │   • tx.Commit                             ─┘
   │       │
   │       ▼
   │     post-commit callbacks fire (if any)
   │
   ▼
Result<Guid>  →  ResultExtensions.ToHttpResult
   │
   ▼
201 Created  /api/v1/orders/{id}    (on success)
4xx ProblemDetails                  (on failure, with code + correlationId)
```

## 6. Aggregate sample — Customer

```csharp
public sealed class Customer : AggregateRoot<CustomerId>, IAuditableEntity, ISoftDeletable
{
    // Private state. No public setters. Mutations only through methods below.
    private readonly List<Address> _addresses = [];

    public PersonName Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public CustomerStatus Status { get; private set; }
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    // Factory enforces invariants and raises the creation event.
    public static Customer Register(PersonName name, Email email)
    {
        CheckRule(new EmailMustBeProvided(email));
        CheckRule(new NameMustBeProvided(name));

        var customer = new Customer(CustomerId.New(), name, email);
        customer.RaiseDomainEvent(new CustomerRegisteredDomainEvent(customer.Id, name.Full, email.Value));
        return customer;
    }

    public void ChangeEmail(Email newEmail)
    {
        if (Email == newEmail) return;            // idempotent
        CheckRule(new CustomerMustBeActive(Status));
        var old = Email;
        Email = newEmail;
        RaiseDomainEvent(new CustomerEmailChangedDomainEvent(Id, old.Value, newEmail.Value));
    }

    public void Deactivate(string reason)
    {
        if (Status == CustomerStatus.Deactivated) return;
        Status = CustomerStatus.Deactivated;
        RaiseDomainEvent(new CustomerDeactivatedDomainEvent(Id, reason));
    }

    // …Rename, AddAddress, RemoveAddress, Suspend, Reactivate
}
```

Notice: no anemia, no public setters, all business logic on the aggregate, factories returning a fully-constructed valid instance, every state change emits a domain event.

## 7. Repositories — write vs. read

```csharp
// Write repository — aggregate-oriented, tracked, used inside a Unit of Work.
public interface IWriteRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task AddAsync(TAggregate aggregate, CancellationToken ct = default);
    void Update(TAggregate aggregate);
    void Remove(TAggregate aggregate);
}

public interface ICustomerRepository : IWriteRepository<Customer, CustomerId>
{
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken ct = default);
}

// Read repository — projection-friendly, AsNoTracking, supports specifications.
public interface IReadRepository<T> where T : class
{
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<TProjection?> FirstOrDefaultAsync<TProjection>(ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<PagedList<TProjection>> PageAsync<TProjection>(ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector, PageRequest page, CancellationToken ct = default);
    Task<long> CountAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default);
}
```

The Domain owns the **interfaces**. Infrastructure provides **implementations** that inherit from `EfWriteRepository<TDb,T,TId>` / `EfReadRepository<TDb,T>`. Modules also expose dedicated **read-model interfaces** (`ICustomerReadModel`, `IOrderReadModel`) where the projection shape is dictated by the UI/use case — the seam for CQRS read/write store splits.

## 8. Unit of Work — nested semantics

```
Outer command handler                          UoW state
──────────────────────                          ──────────
manager.Begin()                                 stack: [root]   (EfCoreUnitOfWork)
  └── handler does work                         
       └── inner command (e.g. mediator.Send)   
            └── UnitOfWorkBehavior begins       stack: [root, child]
                manager.Begin()                              (ChildUnitOfWork,
                                                              shares root's tx)
                  └── inner handler work
                  child.CompleteAsync()         no commit; bubbles post-commit
                                                callbacks to root
            ▲
       ← back to outer handler
root.CompleteAsync()                            SaveChanges → Commit → fire callbacks
                                                stack: []
```

`UnitOfWorkOptions(RequiresNew: true)` forces a new transaction even when nested — useful for audit writes that must survive a rollback of the business transaction.

## 9. Outbox + Inbox

### Outbox table (per module)
```
outbox_messages
─────────────────
id                uuid PK
type              text         AssemblyQualifiedName of the integration event
payload           text         JSON-serialised event
occurred_on_utc   timestamptz
processed_on_utc  timestamptz NULL                 set when published
error             text NULL                        last failure reason
attempts          int
correlation_id    text NULL
```

Indexed on `processed_on_utc, occurred_on_utc` so the poller's hot path is fast.

### Inbox table (per consuming module, when needed)
```
inbox_messages
─────────────────
event_id          uuid PK              dedup key — handler checks before processing
type              text
received_on_utc   timestamptz
processed_on_utc  timestamptz NULL
error             text NULL
attempts          int
```

`IInboxStore.AlreadyProcessedAsync(eventId)` keeps consumers idempotent — replays don't double-cancel orders.

## 10. Cross-cutting building blocks

```
BuildingBlocks.SharedKernel       Result, Error, PagedList, PageRequest, IClock, Guard
BuildingBlocks.Domain             Entity, AggregateRoot, ValueObject, StronglyTypedId,
                                  IDomainEvent, IBusinessRule, IAuditableEntity,
                                  ISoftDeletable, ISpecification, DomainException tree
BuildingBlocks.Application        ICommand/IQuery + handlers, ICurrentUser, IPermissionService,
                                  RequiresPermissionAttribute, IUnitOfWork(Manager|Accessor),
                                  IWriteRepository, IReadRepository, ICacheService,
                                  IFeatureFlags, IDistributedLock, IBackgroundJobScheduler,
                                  IAuditLogger, IModule, pipeline behaviors
BuildingBlocks.EventBus           IIntegrationEvent(Handler|Bus|Queue), IDomainEventDispatcher,
                                  OutboxMessage, InboxMessage, InProcess implementations
BuildingBlocks.UnitOfWork         AmbientUnitOfWorkAccessor, UnitOfWorkManager,
                                  ChildUnitOfWork, IUnitOfWorkFactory
BuildingBlocks.Infrastructure     ModuleDbContext, AuditingInterceptor, SoftDeleteInterceptor,
                                  OutboxInterceptor, EfWriteRepository, EfReadRepository,
                                  SpecificationEvaluator, OutboxProcessor,
                                  EfCoreUnitOfWork(+Factory), InMemoryCacheService,
                                  InMemoryDistributedLock, MicrosoftFeatureFlags,
                                  LoggerAuditLogger, IDataSeeder
BuildingBlocks.Presentation       IModuleEndpoints, ResultExtensions.ToHttpResult,
                                  CorrelationIdMiddleware, GlobalExceptionMiddleware
BuildingBlocks.Observability      UseEnterpriseSerilog, AddEnterpriseTelemetry
BuildingBlocks.MultiTenancy       ITenantContext, NullTenantContext
```

## 11. Architecture tests (what runs on every build)

`tests/ArchitectureTests`:

```
[ModuleIsolationTests]
  Domain_should_not_depend_on_any_other_module                          ← fails if Customers.Domain references Orders.Domain
  Application_can_consume_contracts_and_integration_events_but_not_    ← fails if Orders.Application references Customers.Domain
    other_modules_domain_or_infra
  Domain_should_not_reference_infrastructure_at_all                     ← fails if any Domain references EF/AspNetCore
  Application_should_not_reference_aspnet_or_efcore                     ← fails if any Application references AspNetCore/EF
  Contracts_assemblies_must_be_dependency_free                          ← fails if Contracts has any non-System dependency

[CleanArchitectureTests]
  Aggregate_roots_should_be_sealed
  Domain_events_should_be_sealed_records
  Command_handlers_should_be_internal                                   ← keeps the module's public surface tight
```

If a future PR breaks any of these, CI fails — no architecture drift.

## 12. Decisions log (short)

- **Postgres.** Chosen for `xmin` concurrency tokens, mature `EnableRetryOnFailure`, free, and `schema-per-module` is first-class.
- **MediatR.** Industry-standard CQRS pipeline. The behaviors model (Logging/Tracing/Validation/Authorization/UnitOfWork) is exactly the seam we want.
- **FluentValidation over data annotations.** Composable, easier to test, surfaces every failure at once.
- **Result over exceptions for expected failures.** Exceptions for invariant violations (`DomainException`), `Result` for "user typed a duplicate email." Keeps stack traces meaningful and makes HTTP mapping declarative.
- **AsyncLocal Unit of Work.** ABP-style. Removes a constant parameter-threading tax on handlers without making the transaction implicit-magic.
- **In-process bus + Outbox.** Microservice-ready transport with monolith-simple ops on day one.
- **Strict TreatWarningsAsErrors.** Trades minor friction for many real bugs caught at PR time.
- **Single host process, no Aspire / no service mesh.** Keep the local dev loop fast; Aspire is great but optional.
