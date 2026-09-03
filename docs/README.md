# Baobab SharedKernel Documentation

## Building Microservices with .NET 10, Clean Architecture, and DDD

This is the documentation index for Baobab.SharedKernel — a Clean Architecture
foundation for building CQRS/DDD/event-driven .NET applications of any
shape: microservices, modular monoliths, or a single service.

### What's in it

- **Clean Architecture** — five layers (Domain, Application, Persistence,
  Infrastructure, Presentation) with strict inward dependency flow
- **Domain-Driven Design** — aggregate roots, value objects, domain events
- **Result pattern** — `Result`, `ResultT<T>`, `ValidationResult`,
  `PaginatedResult<T>` in place of exceptions for business-logic failures
- **CQRS** via MediatR, with logging/unit-of-work/validation pipeline behaviors
- **Assembly-aware OutBox pattern** — reliable event publishing, with
  idempotent handler decoration and per-service event isolation
- **Audit trail** — automatic before/after change tracking with user
  attribution, background-job-aware
- **Caching** — Redis-distributed and in-memory implementations of the same
  interface
- **Background jobs** via Hangfire, with Polly retry policies
- **API key/secret** generation and validation helpers
- **Zone-based authorization** on top of standard role checks
- **OpenTelemetry** tracing/metrics, with Sentry (or self-hosted
  GlitchTip) error tracking wired through the same pipeline
- IDs are `Guid`s created via `Guid.CreateVersion7()` — sortable and
  index-friendly, with no third-party ID package

## Table of Contents

1. [Comprehensive Implementation Guide](./comprehensive-implementation-guide.md) — feature-by-feature reference
2. [Getting Started](./getting-started.md) — build your first service step-by-step
3. [Architecture Overview](./architecture-overview.md) — the Clean Architecture structure
4. [Team Architecture Handoff Guide](./team-architecture-handoff-guide.md) — the complete technical reference
5. [Patterns and Best Practices](./patterns-and-practices.md)
6. [Practical Examples](./examples.md)
7. [Troubleshooting](./troubleshooting.md)
8. [Domain Layer Organization Plan](./domain-layer-organization-plan.md)
9. [Blog Series](./blog-series.md) — longer-form writeups of the architecture

## Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL, Redis, RabbitMQ during local development)
- Familiarity with Clean Architecture, DDD, and CQRS helps but isn't required
  — [Getting Started](./getting-started.md) builds the concepts up from
  scratch.

## Quick Start

```bash
git clone https://github.com/barimahyaw/baobab.git
cd baobab
dotnet restore Baobab.sln
dotnet build Baobab.sln
```

NuGet packages (`Baobab.SharedKernel.Domain`, `.Application`,
`.Infrastructure`, `.Persistence`, `.Presentation`) will be published once
the project reaches its first tagged release — see
[CHANGELOG.md](../CHANGELOG.md) for what's landed so far. Until then,
reference the projects directly or via a project-to-project reference from a
git submodule/subtree.

## Learning Path

1. **Start here**: [Architecture Overview](./architecture-overview.md)
2. **Get hands-on**: [Getting Started](./getting-started.md)
3. **Go deep**: [Team Architecture Handoff Guide](./team-architecture-handoff-guide.md)
4. **Master patterns**: [Patterns and Best Practices](./patterns-and-practices.md)
5. **See it in context**: [Practical Examples](./examples.md)

## Contributing

Bug reports, documentation fixes, and code contributions are welcome — see
[CONTRIBUTING.md](../CONTRIBUTING.md).

## License

MIT — see [LICENSE](../LICENSE).
