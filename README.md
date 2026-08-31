# Baobab SharedKernel
=======
# Domain Driven Design, Event Driven with Clean Architecture

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

A Clean Architecture / Domain-Driven Design foundation for building
event-driven .NET microservices — CQRS, an assembly-aware OutBox pattern,
comprehensive auditing, and a Result-based error-handling model, all wired
together so a new service can start on business logic instead of
infrastructure plumbing.

This isn't a scaffold of interfaces waiting to be implemented — every layer
below is a working implementation you can reference, extend, or drop into a
service today.

## Why this exists

Most "Clean Architecture starter" repos give you folder names and a few
marker interfaces. This gives you the parts that are actually tedious to get
right: an OutBox implementation that won't double-process events when two
services share a database, an audit trail that distinguishes background jobs
from user requests, a Result type hierarchy that composes across validation
and pagination, and Guid generation that stays sortable in the database
instead of fragmenting your indexes.

## Architecture

```
SharedKernel/
├── Baobab.SharedKernel.Domain          Entities, aggregates, value objects,
│                                       domain events, the Result pattern
├── Baobab.SharedKernel.Application     CQRS (ICommand/IQuery), pipeline
│                                       behaviors, service abstractions
├── Baobab.SharedKernel.Persistence     EF Core, OutBox pattern, audit
│                                       trail, specifications, unit of work
├── Baobab.SharedKernel.Infrastructure  Caching, background jobs, messaging,
│                                       resilience, external services
└── Baobab.SharedKernel.Presentation    API controllers, Minimal APIs,
                                        exception handling, versioning
```

Dependencies point inward only: `Presentation -> Infrastructure ->
Persistence -> Application -> Domain`. The Domain layer has zero project
dependencies.

## What's implemented

- **Domain-Driven Design primitives** — `AggregateRoot`, `Entity`,
  `EntityExtra` (audit fields), `ValueObject`, with rich value objects
  (`Money`, `EmailAddress`, `PhoneNumber`, `GhanaCardPersonalIdentificationNumber`)
- **CQRS** — `ICommand`/`IQuery` interfaces over MediatR, with
  `ValidationPipelineBehavior` (FluentValidation -> Result), `LoggingPipelineBehavior`,
  and `UnitOfWorkPipelineBehavior` running in sequence on every request
- **Result pattern** — `Result`, `ResultT<T>`, `ValidationResult`,
  `PaginatedResult<T>` in place of exceptions for expected business outcomes
- **Assembly-aware OutBox pattern** — domain events are captured in the same
  transaction as your data, then published by a background job. The
  `ExecutingAssembly` field means multiple services sharing one database
  only ever process their own events. Idempotent handler decoration means a
  retried message doesn't re-run side effects.
- **Guid v7 identifiers** — every ID is a `Guid` created via
  `Guid.CreateVersion7()`: sortable and time-ordered like a ULID, but a
  native .NET type with no third-party dependency
- **Audit trail** — `AuditableContext<T>` captures before/after values, the
  acting user, and timestamps automatically on every `SaveChangesAsync`,
  with explicit handling for background jobs running without a user context
- **Specification pattern** — `HeroSpecification<T>` for composable,
  reusable query logic with includes and ordering
- **Multi-strategy caching** — Redis-backed and in-memory implementations
  of the same `ICacheManager` interface
- **Background jobs** — Hangfire integration with Polly retry policies
- **Messaging** — MassTransit + RabbitMQ for integration events
- **Notifications** — email (Amazon SES or SMTP), SMS, and push, each with
  a sync path and a MassTransit-published integration-event path
- **File storage** — AWS S3 upload/download behind `IAmazonSimpleStorageService`
- **API security** — JWT bearer authentication, multi-factor API key/secret
  generation, zone-based authorization layered on top of role checks
- **Observability** — OpenTelemetry tracing/metrics across ASP.NET Core, EF
  Core, Hangfire, gRPC, and Redis, plus Sentry (or self-hosted
  GlitchTip) error tracking correlated to the same traces
- **Presentation extras** — API versioning (controllers and Minimal APIs),
  Swagger with JWT bearer auth wired in, rate limiting, and an RFC 7807
  global exception handler

## Getting Started

```bash
git clone https://github.com/barimahyaw/baobab.git
cd baobab
dotnet restore Baobab.sln
dotnet build Baobab.sln
```

NuGet packages aren't published yet — for now, reference the projects
directly or pull the `SharedKernel/` folder into your solution. See
[docs/getting-started.md](./docs/getting-started.md) for a full walkthrough
of building a service on top of this foundation.

## A taste of the patterns

**Rich domain model, raising an event:**
```csharp
public class Order : AggregateRoot
{
    public Result AddItem(ProductId productId, Money unitPrice, int quantity)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Errors.OrderErrors.CannotModifyConfirmedOrder);

        var item = OrderItem.Create(Guid.CreateVersion7(), productId, unitPrice, quantity);
        _items.Add(item);

        RaiseDomainEvent(new OrderItemAddedDomainEvent(Id, productId, quantity));
        return Result.Success();
    }
}
```

**CQRS command handler, Result all the way down:**
```csharp
public record CreateUserCommand(string Email, string FirstName) : ICommand<Guid>;

public class CreateUserCommandHandler(AppDbContext dbContext) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<IResult<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Validate(request.Email);
        if (!emailResult.Succeeded) return ResultT<Guid>.Fail(emailResult.Messages);

        var user = User.Create(EmailAddress.Create(request.Email), request.FirstName);
        await dbContext.Users.AddAsync(user, cancellationToken);
        // UnitOfWorkPipelineBehavior calls SaveChangesAsync after the handler returns

        return ResultT<Guid>.Success(user.Id);
    }
}
```

**Minimal API controller:**
```csharp
[ApiVersion("1.0")]
public class UsersController : BaseApiController<UsersController>
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await Mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetUser), new { id = result.Value }, result.Value)
            : BadRequest(result.Messages);
    }
}
```

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](./docs/getting-started.md) | Build your first microservice step-by-step |
| [Architecture Overview](./docs/architecture-overview.md) | The Clean Architecture layers and how they relate |
| [Team Architecture Handoff Guide](./docs/team-architecture-handoff-guide.md) | Complete technical reference, layer by layer |
| [Patterns & Practices](./docs/patterns-and-practices.md) | Proven patterns with real examples |
| [Practical Examples](./docs/examples.md) | Complete real-world scenarios |
| [Troubleshooting](./docs/troubleshooting.md) | Common issues and how to resolve them |
| [Full documentation index](./docs/README.md) | Everything, one level up |

## Project Status

This is an actively evolving personal project — the core is stable and used
as the foundation for real services, but public NuGet packages and a
templated `dotnet new` experience aren't published yet. Track progress in
[CHANGELOG.md](./CHANGELOG.md).

## Contributing

Bug reports, documentation fixes, and code contributions are welcome — see
[CONTRIBUTING.md](./CONTRIBUTING.md).

## Security

See [SECURITY.md](./SECURITY.md) for how to report a vulnerability.

## License

MIT — see [LICENSE](./LICENSE).
