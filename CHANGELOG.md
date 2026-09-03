# Changelog

All notable changes to Baobab.SharedKernel are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **Rebrand**: renamed the project's namespaces from an earlier, inconsistent
  naming scheme to a single `Baobab.SharedKernel.*` identity across every
  project, namespace, and package.
- **Target framework**: moved from .NET 9 to .NET 10.
- **ID strategy**: replaced the `Ulid` value type with native `Guid`, generated via
  `Guid.CreateVersion7()` for the same sortable, time-ordered property Ulid provided.
- Introduced `Directory.Build.props`/`Directory.Packages.props` for centralized
  build settings and package versioning across all 5 projects.
- Consolidated `AuditableContext`/`AuditableIdentityDbContext` (moved to
  `Audits/Contexts/`, inlined the audit-entry-building logic previously split
  into a separate `AuditHelper` class) with hardened `SYSTEM_ADMIN_ID` handling
  and structured logging.
- Renamed `AddGlitchTipConfiguration` to `AddSentryConfiguration` (`GLITCHTIP_DSN`
  -> `SENTRY_DSN`); still Sentry-SDK/wire-compatible with self-hosted GlitchTip.
- Renamed `ISchemaStringValue` to `IProjectStringValue`.

### Added
- OutBox idempotence is now functional: `AddOutboxIdempotentConfig<TDbContext>`
  decorates every registered domain event handler via reflection (previously a
  no-op stub pending an unused Scrutor dependency), correlated through the new
  `IOutboxMessageContext` rather than the event's own embedded ID.
- Assembly-aware OutBox: `OutboxMessage.ExecutingAssembly` lets multiple
  services share one database without processing each other's events.
- AWS S3 file storage (`IAmazonSimpleStorageService`), push notifications
  (`IPushNotificationService`), API key/secret generation (`ApiKeyService`,
  `ApiSecretService`), and `SharedKernelTelemetry` (OpenTelemetry
  `ActivitySource`/`Meter`) for custom spans and metrics.
- Rate limiting configuration (`AddRateLimitConfig`).
- `ICurrentUserService` gained `TraceIdentifier`, `Channel`, `DeviceId`,
  `AppVersion`, `DeviceVersion`, and `BearerToken`, alongside the existing
  `IpAddress`/`UserAgent` (now properties rather than methods).

## [1.0.5] - 2024-01-15

### Added
- **Domain Layer**: rich value objects with validation (Money, EmailAddress,
  GhanaCardPersonalIdentificationNumber), a Result-pattern family (Result,
  Result\<T\>, ValidationResult, PaginatedResult\<T\>), reflection-based
  `EventFactory` for cross-assembly domain event reconstruction, a
  categorized error system, and `EntityExtra` for audit fields.
- **Application Layer**: full CQRS via MediatR, pipeline behaviors
  (Validation, Logging, UnitOfWork), a rich user context service with
  zone-based authorization, API key/secret management.
- **Infrastructure Layer**: Redis + memory caching, Amazon SES email,
  Hangfire background jobs with Polly retries, comprehensive claims-based
  `CurrentUserService`.
- **Persistence Layer**: assembly-aware OutBox pattern, audit system via
  `AuditableContext`, `HeroSpecification`-based query composition, automatic
  domain-event-to-outbox-message conversion.
- **Presentation Layer**: `BaseApiController`, global exception handling,
  Minimal API endpoint discovery, API versioning.

## [1.0.0] - 2023-12-01

### Added
- Initial 5-layer Clean Architecture structure.
- Domain-Driven Design primitives (`Entity`, `AggregateRoot`, `ValueObject`).
- Basic CQRS pattern and OutBox-based event publishing.
- PostgreSQL support via Entity Framework Core.

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).
