# Baobab SharedKernel - Comprehensive Architecture & Implementation Guide

> **Purpose:** This document provides a complete technical reference for teams implementing .NET services — microservices, modular monoliths, or a single service — using the Baobab SharedKernel foundation. It covers every architectural pattern, code standard, configuration requirement, and implementation workflow needed to build production-grade services.

> **Target Framework:** .NET 10.0 | **Architecture:** Clean Architecture + DDD + CQRS
> **SharedKernel Version:** 1.0.0 (all five projects versioned together)

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Solution Structure & Layer Dependencies](#2-solution-structure--layer-dependencies)
3. [Domain Layer](#3-domain-layer)
4. [Application Layer](#4-application-layer)
5. [Persistence Layer](#5-persistence-layer)
6. [Infrastructure Layer](#6-infrastructure-layer)
7. [Presentation Layer](#7-presentation-layer)
8. [Building a New Service (Step-by-Step)](#8-building-a-new-service-step-by-step)
9. [Code Standards & Conventions](#9-code-standards--conventions)
10. [Environment Variables & Configuration Reference](#10-environment-variables--configuration-reference)
11. [Technology Stack & NuGet Packages](#11-technology-stack--nuget-packages)

---

## 1. Architecture Overview

### 1.1 Clean Architecture Layers

The SharedKernel follows a strict 5-layer Clean Architecture with inward-pointing dependencies:

```
                    +---------------------------+
                    |      Presentation         |  API Controllers, Minimal APIs,
                    |  (Baobab.SharedKernel|   Swagger, Versioning, Exception
                    |       .Presentation)       |   Handling, Rate Limiting
                    +-------------+-------------+
                                  |
                    +-------------v-------------+
                    |      Infrastructure        |  Caching (Redis/Memory), Hangfire,
                    |  (Baobab.SharedKernel|   AWS S3, Email/SMS/Push, JWT Auth,
                    |     .Infrastructure)       |   Polly, OpenTelemetry, gRPC, Sentry
                    +-------------+-------------+
                                  |
                    +-------------v-------------+
                    |       Persistence          |  EF Core, OutBox Pattern, Audit
                    |  (Baobab.SharedKernel|   System, Specifications, Unit of
                    |      .Persistence)         |   Work, Repository, Idempotence
                    +-------------+-------------+
                                  |
                    +-------------v-------------+
                    |       Application          |  CQRS (Commands/Queries), Pipeline
                    |  (Baobab.SharedKernel|   Behaviors, Service Abstractions,
                    |      .Application)         |   MassTransit, Telemetry
                    +-------------+-------------+
                                  |
                    +-------------v-------------+
                    |         Domain             |  Entities, Aggregates, Value Objects,
                    |  (Baobab.SharedKernel|   Domain Events, Result Pattern,
                    |        .Domain)            |   Error Types, Notifications
                    +---------------------------+
```

### 1.2 Dependency Flow

```
Presentation --> Infrastructure --> Persistence --> Application --> Domain
```

Each outer layer depends only on the layer directly beneath it. The Domain layer has zero project dependencies (only MediatR.Contracts and Newtonsoft.Json — identifiers are native `Guid`, generated via `Guid.CreateVersion7()`).

### 1.3 Core Architectural Patterns

| Pattern | Purpose | Location |
|---------|---------|----------|
| **CQRS** | Separate read/write paths | Application Layer |
| **Domain-Driven Design** | Rich domain models with aggregate boundaries | Domain Layer |
| **OutBox Pattern** | Reliable event publishing with assembly isolation | Persistence Layer |
| **Result Pattern** | Explicit error handling without exceptions | Domain Layer |
| **Specification Pattern** | Composable, reusable query logic | Persistence Layer |
| **Unit of Work** | Transaction boundary management | Persistence + Application |
| **Pipeline Behaviors** | Cross-cutting concerns (validation, logging, transactions) | Application Layer |
| **Decorator Pattern** | Idempotent domain event processing | Persistence Layer |
| **Repository Pattern** | Data access abstraction | Persistence Layer |
| **Event-Driven Architecture** | Async messaging via MassTransit + RabbitMQ | Application + Infrastructure |

---

## 2. Solution Structure & Layer Dependencies

### 2.1 Project Layout

```
Baobab.sln
+-- SharedKernel/
|   +-- Baobab.SharedKernel.Domain/
|   |   +-- Primitives/           # Entity, AggregateRoot, EntityExtra, ValueObject, DomainEvent
|   |   +-- Primitives/Factory/   # EventFactory (reflection-based event reconstruction)
|   |   +-- Results/              # Result, ResultT, ValidationResult, PaginatedResult, Error
|   |   +-- ValueObjects/         # EmailAddress, PhoneNumber, Money, UserId, FirstName, etc.
|   |   +-- Enums/                # SortDirection
|   |   +-- Requests/             # PaginatedRequest, UploadRequest
|   |   +-- Notifications/        # Notification entity, events, repository interface
|   |   +-- Lookups/              # LookupType, LookupValue
|   |   +-- Errors.cs             # Centralized domain error definitions
|   |
|   +-- Baobab.SharedKernel.Application/
|   |   +-- Abstractions/Data/        # IUnitOfWork, ICacheManager
|   |   +-- Abstractions/Messaging/   # ICommand, IQuery, IPaginatedQuery + handlers
|   |   +-- Abstractions/Services/    # ICurrentUserService, notification services, API key/secret
|   |   +-- Behaviors/                # ValidationPipeline, LoggingPipeline, UnitOfWorkPipeline
|   |   +-- Telemetry/                # SharedKernelTelemetry (OpenTelemetry sources)
|   |   +-- DependencyInjection.cs    # MediatR, FluentValidation, MassTransit registration
|   |
|   +-- Baobab.SharedKernel.Persistence/
|   |   +-- Audits/                    # Audit entity, AuditEntry, AuditType enum
|   |   +-- Audits/Contexts/           # AuditableContext<T>, AuditableIdentityDbContext<T>
|   |   +-- OutBox/                    # OutboxMessage, OutboxMessageConsumer
|   |   +-- OutBox/Interceptors/       # ConvertDomainEventsToOutboxMessagesInterceptor
|   |   +-- OutBox/Idempotence/        # IdempotentDomainEventHandler, IOutboxMessageContext
|   |   +-- Configurations/            # EF Core entity configs (Outbox, Audit, Lookup, Notification)
|   |   +-- Repositories/             # NotificationRepository
|   |   +-- Specifications/           # ISpecification, HeroSpecification, QueryableExtensions
|   |   +-- UnitOfWork.cs             # UnitOfWork<T> implementation
|   |   +-- DependencyInjection.cs    # DB, UnitOfWork, Idempotent config registration
|   |
|   +-- Baobab.SharedKernel.Infrastructure/
|   |   +-- BackgroundJobs/            # OutBoxMessagesProcessingJob, HangfireAuthorizationFilter
|   |   +-- Cache/                     # DistributedCacheManager (Redis), MemoryCacheManager
|   |   +-- Resilience/               # PollyPolicy (retry with exponential backoff)
|   |   +-- Services/AmazonStorageService/  # AWS S3 file storage
|   |   +-- Services/NotificationService/   # Email, SMS, Push notification services
|   |   +-- Services/CurrentUserService.cs  # HTTP context user extraction
|   |   +-- DependencyInjection.cs          # All infrastructure registrations
|   |
|   +-- Baobab.SharedKernel.Presentation/
|       +-- MinimalApi/                # IEndpoint, EndpointExtensions
|       +-- BaseApiController.cs       # Base controller with MediatR + Logger
|       +-- GlobalExceptionHandler.cs  # RFC 7807 ProblemDetails error handler
|       +-- DependencyInjection.cs     # Swagger, Serilog, versioning, rate limiting
```

### 2.2 Inter-Project References

```
Domain          --> (no project references)
Application     --> Domain
Persistence     --> Application
Infrastructure  --> Persistence
Presentation    --> Infrastructure
```

---

## 3. Domain Layer

### 3.1 Entity Hierarchy

```
Entity (abstract)                    # Base equality, reference-based identity
  +-- EntityExtra                    # Audit fields: CreatedUserId, CreatedAtUtc,
  |                                  #   LastModifiedUserId, LastModifiedAtUtc, IsActive
  +-- AggregateRoot (abstract)       # Domain event management: RaiseDomainEvent(),
                                     #   GetDomainEvents(), ClearDomainEvents()

ValueObject (abstract)               # Value-based equality via GetAtomicValues()
```

**Key classes and their roles:**

| Class | Namespace | Purpose |
|-------|-----------|---------|
| `Entity` | `Primitives` | Base class with `IEquatable<Entity>` implementation |
| `EntityExtra` | `Primitives` | Adds `CreatedUserId`, `CreatedAtUtc`, `LastModifiedUserId`, `LastModifiedAtUtc`, `IsActive` audit fields. All properties use `UserId` value object. |
| `AggregateRoot` | `Primitives` | Extends `EntityExtra`. Manages `List<IDomainEvent>` internally. Methods: `RaiseDomainEvent()`, `GetDomainEvents()`, `ClearDomainEvents()` |
| `ValueObject` | `Primitives` | Abstract class. Equality via `GetAtomicValues()` sequence comparison. `GetHashCode()` aggregates atomic values. |

### 3.2 Domain Events

```csharp
// Interface - extends MediatR's INotification
public interface IDomainEvent : INotification { }

// Base record with ULID identity
public record DomainEvent(Guid Id) : IDomainEvent;
```

**EventFactory** (`Primitives/Factory/EventFactory.cs`) - Reconstructs domain events from serialized OutBox messages using reflection:

```csharp
public static IDomainEvent CreateEventTypeUsingReflection(
    string assembly, string typeName, string jsonContent)
```

- Loads assembly dynamically via `Assembly.Load(assembly)`
- Resolves type via `assembly.GetType(typeName)`
- Deserializes JSON using `Newtonsoft.Json`
- Validates the type implements `IDomainEvent`

**Domain Event Lifecycle:**

```
1. Aggregate raises event    -->  RaiseDomainEvent(new OrderPlacedEvent(Id))
2. Events stored internally  -->  _domainEvents.Add(domainEvent)
3. SaveChanges interceptor   -->  Converts events to OutboxMessage records
4. Events cleared            -->  ClearDomainEvents()
5. Background job processes  -->  OutBoxMessagesProcessingJob deserializes & publishes
6. MediatR dispatches        -->  INotificationHandler<TEvent> receives event
7. Idempotency tracked       -->  OutboxMessageConsumer prevents duplicates
```

### 3.3 Result Pattern

The Result pattern replaces exceptions for business logic failures. **Never throw exceptions for expected business outcomes.**

| Type | Usage | Key Properties |
|------|-------|----------------|
| `Result` | Success/failure without data | `Succeeded`, `Messages` (Error[]) |
| `ResultT<T>` | Success/failure with typed data | `Succeeded`, `Messages`, `Data` |
| `ValidationResult` | Validation failures (always fails) | Inherits Result, implements `IValidationResult` |
| `ValidationResultT<T>` | Typed validation failures | Inherits ResultT<T>, implements `IValidationResult` |
| `PaginatedResult<T>` | Paginated list results | `Data`, `CurrentPage`, `TotalPages`, `TotalCount`, `PageSize`, `HasPreviousPage`, `HasNextPage` |
| `Error` | Structured error record | `Code` (snake_case string), `Message` (human-readable) |

**Factory methods on Result:**

```csharp
// Failure
Result.Fail()
Result.Fail(params Error[] errors)
Result.FailAsync()

// Success
Result.Success()
Result.Success(params Error[] messages)  // can include warnings

// Generic
ResultT<T>.Success(T data)
ResultT<T>.Success(T data, params Error[] messages)
ResultT<T>.Fail(params Error[] errors)

// Validation
ValidationResult.WithErrors(Error[] errors)
ValidationResultT<T>.WithErrors(Error[] errors)

// Pagination
PaginatedResult<T>.Success(List<T> data, int count, int page, int pageSize)
PaginatedResult<T>.Failure(params Error[] errors)
```

### 3.4 Value Objects

All value objects follow this standard pattern:

1. **Sealed class** inheriting from `ValueObject`
2. **Private constructor** (creation only through factory)
3. **Static `Validate()` method** returning `Result` with domain errors
4. **Static `Create()` method** for instantiation (call Validate first)
5. **`GetAtomicValues()` override** for equality
6. **Explicit/implicit operator** for type conversion

| Value Object | Validation Rules | Conversion |
|-------------|-----------------|------------|
| `EmailAddress` | Not null, regex `^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$`, 6-100 chars | `explicit string` |
| `PhoneNumber` | Value: 6-50 chars, digits only. CountryCode: 4 chars, starts with `+`, digits 2-4 | `explicit string` |
| `FirstName` | Not null/whitespace, max 50 chars | N/A |
| `LastName` | Not null/whitespace, max 50 chars | N/A |
| `OtherName` | Optional (null allowed), max 50 chars if provided | N/A |
| `UserId` | Non-empty, non-default Guid | `implicit Guid` (bidirectional) |
| `Money` | Currency not null, Amount > 0. `Add`/`Subtract` enforce currency match. | `explicit decimal` |
| `GhanaCardPersonalIdentificationNumber` | Exactly 15 chars, regex `^[A-Z]{3}-\d{9}-\d$` (format: `GHA-123456789-0`) | `explicit string` |

**Example - Creating a value object:**

```csharp
// Always validate before creating
Result validation = EmailAddress.Validate(emailString);
if (!validation.Succeeded)
    return Result.Fail(validation.Messages);

EmailAddress email = EmailAddress.Create(emailString);
```

### 3.5 Centralized Errors

All domain errors are defined in `Errors.cs` as nested sealed records with snake_case codes:

```csharp
// Usage
Result.Fail(Errors.EmailAddressErrors.EmailAddressInvalid);
Result.Fail(Errors.MoneyErrors.CurrencyMismatch);
```

**Error code format:** `snake_case` (e.g., `"email_address_invalid"`, `"currency_mismatch"`, `"user_id_empty"`)

### 3.6 Notification Domain

```csharp
// Notification entity with lifecycle management
Notification.Create(contact, message, subject)
notification.MarkAsDelivered()      // Sets IsDelivered=true, DeliveredAtUtc
notification.MarkAsFailed(error)    // Sets IsDelivered=false, DeliveryError
notification.SetNotificationType(NotificationType.Email)

// Notification types
enum NotificationType { Email = 1, SMS, InApp, Push }

// Integration events (published via MassTransit)
record EmailNotificationIntegratedEvent(string Email, string Subject, string Message, string Product, byte[]? Attachment, string? AttachmentName)
record SmsNotificationIntegratedEvent(string[] Recipients, string Message, string SenderId, string ProductId)
record PushNotificationIntegratedEvent(string[] To, string Message, string ProductId)
```

### 3.7 Lookup Domain

Reference data management with `LookupType` (categories) containing `LookupValue` entries. Both inherit from `EntityExtra` for full audit trail.

```csharp
LookupType: Id (Guid), TypeName (150 chars), Description (255 chars), LookupValues (HashSet)
LookupValue: Id (Guid), ValueName, ValueDescription, LookupTypeId
```

### 3.8 Shared Request Types

```csharp
// Pagination request parameters
record PaginatedRequest {
    string? SearchTerm, int PageSize, int PageNumber,
    string? SortLabel, SortDirection SortDirection, bool IsDownloadRequest
}

// File upload contract
record UploadRequest(byte[] FileBytes, string FileName);

// Sort direction
enum SortDirection { None = 0, Ascending = 1, Descending = 2 }
```

---

## 4. Application Layer

### 4.1 CQRS Interfaces

The CQRS pattern separates write operations (Commands) from read operations (Queries). All return types are wrapped in the domain's `IResult` type.

```csharp
// COMMANDS (write operations)
public interface ICommand : IRequest<IResult> { }
public interface ICommand<TResponse> : IRequest<IResult<TResponse>> { }

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, IResult>
    where TCommand : ICommand { }
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, IResult<TResponse>>
    where TCommand : ICommand<TResponse> { }

// QUERIES (read operations)
public interface IQuery<TResponse> : IRequest<IResult<TResponse>> { }

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, IResult<TResponse>>
    where TQuery : IQuery<TResponse> { }

// PAGINATED QUERIES
public interface IPaginatedQuery<TResponse> : IRequest<PaginatedResult<TResponse>> { }

public interface IPaginatedQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, PaginatedResult<TResponse>>
    where TQuery : IPaginatedQuery<TResponse> { }
```

**Example - Implementing a command:**

```csharp
// Command definition
public record CreateUserCommand(string Email, string FirstName) : ICommand<Guid>;

// Validator (FluentValidation)
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
    }
}

// Handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<IResult<Guid>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Validate(request.Email);
        if (!emailResult.Succeeded) return ResultT<Guid>.Fail(emailResult.Messages);

        var user = User.Create(EmailAddress.Create(request.Email), /* ... */);
        // persist and return
        return ResultT<Guid>.Success(user.Id);
    }
}
```

**Example - Implementing a query:**

```csharp
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<IResult<UserDto>> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FindAsync(request.Id);
        if (user is null) return ResultT<UserDto>.Fail(new Error("not_found", "User not found"));
        return ResultT<UserDto>.Success(new UserDto(user));
    }
}
```

**Example - Implementing a paginated query:**

```csharp
public record GetUsersQuery(int Page, int PageSize) : IPaginatedQuery<UserDto>;

public class GetUsersQueryHandler : IPaginatedQueryHandler<GetUsersQuery, UserDto>
{
    public async Task<PaginatedResult<UserDto>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Select(u => new UserDto(u))
            .ToPaginatedListAsync(request.Page, request.PageSize);
    }
}
```

### 4.2 MediatR Pipeline Behaviors

Pipeline behaviors intercept every request flowing through MediatR. They execute in registration order:

```
Request
  |
  v
[1. LoggingPipelineBehavior]      -->  Logs request start with type name and UTC timestamp
  |
  v
[2. UnitOfWorkPipelineBehavior]   -->  Opens TransactionScope for commands (skips queries)
  |
  v
[3. ValidationPipelineBehavior]   -->  Runs FluentValidation; returns ValidationResult on failure
  |
  v
[Handler]                         -->  Your business logic executes here
  |
  v
[2. UnitOfWorkPipelineBehavior]   -->  Calls SaveChangesAsync(), completes TransactionScope
  |
  v
[1. LoggingPipelineBehavior]      -->  Logs completion or failure with error details
  |
  v
Response
```

#### Validation Pipeline Behavior

- Collects all `IValidator<TRequest>` implementations for the request type
- Runs all validators, collects failures
- Converts FluentValidation failures to domain `Error` objects (using `PropertyName` as code)
- Deduplicates errors
- Returns `ValidationResult.WithErrors()` or `ResultT<T>.Fail()` depending on response type
- **If no validators exist or all pass**, the request proceeds to the handler

#### Logging Pipeline Behavior

- Logs `Information`: `"Starting request {RequestType}, {@DateTimeUtc}"` before handler
- Logs `Warning`: `"Request failure {RequestType}, {@Errors}, {@DateTimeUtc}"` on failure
- Logs `Information`: `"Completed request {RequestType}, {@DateTimeUtc}"` on success

#### Unit of Work Pipeline Behavior

- Detects if request is a Command (vs Query)
- For commands: wraps handler in `TransactionScope` with `TransactionScopeAsyncFlowOption.Enabled`
- Calls `IUnitOfWork.SaveChangesAsync()` after handler completes
- Calls `transactionScope.Complete()` to commit
- For queries: passes through without transaction

### 4.3 Service Abstractions

#### ICurrentUserService

Provides authenticated user context extracted from JWT claims and HTTP headers:

```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }                                      // From ClaimTypes.NameIdentifier
    string? UserName { get; }                                  // From ClaimTypes.Email
    List<KeyValuePair<string, string>> Claims { get; }         // All JWT claims
    string? UserRegionId { get; }                              // From "RegionId" claim
    bool IsInRole(string role);                                // Role membership check
    bool IsInAnyRole(List<string> roles);                      // Multi-role OR check
    bool IsInZone(string zone);                                // Zone access check
    List<string> UserZones();                                  // All user zones
    string Role();                                             // Primary role
    string? IpAddress { get; }                                 // Client IP
    string? UserAgent { get; }                                 // User-Agent header
    string? TraceIdentifier { get; }                           // Distributed trace ID
    string? Channel { get; }                                   // "Channel" header
    string? DeviceId { get; }                                  // "Device-Id" header
    string? AppVersion { get; }                                // "App-Version" header
    string? DeviceVersion { get; }                             // "Device-Version" header
    string? BearerToken { get; }                               // Extracted JWT token
}
```

#### Notification Services

```csharp
// Email (supports attachments, published via MassTransit)
public interface IEmailNotificationService
{
    Task PublishNotificationAsync(string email, string subject, string message,
        string ProductId, byte[] attachment = default!, string attachmentName = default!);
}

// SMS (sync, background, and MassTransit publish)
public interface ISMSNotificationService
{
    Task SendSMSAsync(string[] to, string message);
    Task SendSMSInBackgroundAsync(string[] to, string message);
    Task PublishNotificationAsync(string[] to, string message, string senderId, string productId);
}

// Push notifications (via MassTransit)
public interface IPushNotificationService
{
    Task PublishNotificationAsync(string[] to, string message, string productId);
}
```

#### File Storage

```csharp
public interface IAmazonSimpleStorageService
{
    Task<(bool, string)> Upload(IFormFile file);
    Task<(bool, string)> Upload(byte[] fileBytes, string fileName);
    Task<byte[]> Download(string fileName, string contentType, string? bucketName = null);
    Task<(bool, string)> ListUpload(List<IFormFile> file);
    Task<(bool, string)> ListUpload(List<UploadRequest> req);
}
```

#### Caching

```csharp
public interface ICacheManager
{
    bool Cache<T>(string key, T t, TimeSpan timeSpan);   // Set with expiry
    bool Cache<T>(string key, T value);                   // Set without expiry
    T GetCache<T>(string key);                            // Get by key
    (bool exist, bool success) Remove(string key);        // Remove with existence check
}
```

#### API Key/Secret Authentication

Static utility classes for multi-factor API key security:

```csharp
// API Key format: Base64({API_KEY}_{userName}_{Guid}_{API_SECRET}_{accountId}_{keyName})
// Header: X-Api-Key
ApiKeyService.GenerateApiKey(string userName, Guid accountId, string keyName) -> string
ApiKeyService.IsApiKeyValid(HttpContext context) -> bool
ApiKeyService.GetAccountIdFromApiKey(HttpContext context) -> string

// API Secret format: Base64({API_SECRET}_{userName}_{Guid})
// Header: X-Api-Secret
ApiSecretService.GenerateApiSecret(string userName) -> string
ApiSecretService.IsApiSecretValid(HttpContext context) -> bool
```

### 4.4 Dependency Injection Registration

```csharp
// Register MediatR + pipeline behaviors from your service assembly
services.AddMediatorConfig<Program>();

// Register FluentValidation validators from your service assembly
services.AddAssemblyValidator<Program>();

// Register MassTransit + RabbitMQ with consumer discovery
services.AddMassTransitRabbitMQConfig<Program>(configuration);

// Register MassTransit + RabbitMQ without consumer discovery
services.AddMassTransitRabbitMQConfig(configuration);

// Register event consumers from assembly
services.AddEventConsumersFromAssembly<Program>();
```

### 4.5 Telemetry

```csharp
public static class SharedKernelTelemetry
{
    public const string SourceName = "Baobab.SharedKernel";
    public static readonly ActivitySource ActivitySource = new(SourceName);  // Distributed tracing
    public static readonly Meter Meter = new(SourceName);                    // Metrics
}

// Usage in handlers
using var activity = SharedKernelTelemetry.ActivitySource.StartActivity("ProcessPayment");
SharedKernelTelemetry.Meter.CreateCounter<int>("payments_processed").Add(1);
```

---

## 5. Persistence Layer

### 5.1 OutBox Pattern (Assembly-Aware Reliable Event Publishing)

The OutBox pattern guarantees domain events are published reliably, even if the message bus is temporarily unavailable. Events are stored in the same database transaction as the business data.

#### OutboxMessage Entity

```csharp
public class OutboxMessage : Entity
{
    Guid Id                          // Unique message identifier
    string Type                      // Full type name of domain event
    string Assembly                  // Assembly where event is defined
    string ExecutingAssembly         // Assembly of the consuming application
    string Content                   // JSON-serialized domain event (TypeNameHandling.All)
    DateTime OccurredOnUtc           // When the event was raised
    DateTime? ProcessedDateUtc       // When successfully processed (null = unprocessed)
    DateTime? ProcessLastAttemptOnUtc // Last processing attempt timestamp
    int ProcessingAttempts           // Retry counter
    string? Error                    // Error message if processing failed
}
```

#### OutboxMessageConsumer (Idempotency Tracking)

```csharp
public class OutboxMessageConsumer
{
    Guid Id          // References OutboxMessage.Id
    string Name      // Handler class name
    // Composite key: (Id, Name)
}
```

#### How It Works

```
1. INTERCEPT: ConvertDomainEventsToOutboxMessagesInterceptor hooks into SaveChangesAsync()
   - Extracts all AggregateRoot entities from change tracker
   - Collects domain events from each aggregate
   - Clears events from aggregates
   - Serializes each event to OutboxMessage (JSON with TypeNameHandling.All)
   - Records Type, Assembly, ExecutingAssembly, Content, OccurredOnUtc
   - Adds OutboxMessage entities to DbContext
   - All committed in same transaction as business data

2. PROCESS: OutBoxMessagesProcessingJob (Hangfire recurring job)
   - Queries unprocessed messages matching current assembly (ExecutingAssembly filter)
   - Processes in batches of 20, ordered by ProcessedDateUtc
   - For each message:
     a. Applies Polly retry (3 attempts, 50ms exponential backoff)
     b. Sets message ID in IOutboxMessageContext
     c. Deserializes event via EventFactory.CreateEventTypeUsingReflection()
     d. Publishes via MediatR IPublisher
     e. Marks ProcessedDateUtc on success, or records Error on failure

3. IDEMPOTENT HANDLING: IdempotentDomainEventHandler<TEvent, TDbContext>
   - Decorates all INotificationHandler<IDomainEvent> implementations automatically
   - Before executing handler:
     a. Checks OutboxMessageConsumer for (messageId, handlerName)
     b. If found: skips (already processed)
     c. If not: executes handler, adds consumer record
```

**Assembly Isolation:** The `ExecutingAssembly` filter ensures that when multiple microservices share the same database, each service only processes its own events. This is critical for multi-service deployments.

### 5.2 Audit System

The audit system automatically tracks all entity changes with full before/after values.

#### Audit Entity

```csharp
public sealed class Audit : Entity
{
    Guid Id                    // Unique audit record
    Guid UserId                // Who made the change
    string? Type               // "Create", "Update", or "Delete"
    string? TableName          // Affected database table
    DateTime DateTime          // When the change occurred
    string? OldValues          // JSON of previous values
    string? NewValues          // JSON of new values
    string? AffectedColumns    // JSON array of changed column names
    string? PrimaryKey         // JSON of primary key values
}
```

#### AuditableContext<T>

Your DbContext should inherit from `AuditableContext<T>` (or `AuditableIdentityDbContext<TUser, TRole, TKey>` for Identity-enabled contexts).

**Automatic behavior on SaveChangesAsync:**

1. Retrieves current user from `ICurrentUserService`
2. For background jobs without user context: reads `SYSTEM_ADMIN_ID` environment variable
3. For `EntityState.Added` entities (EntityExtra): sets `CreatedAtUtc`, `CreatedUserId`, `IsActive = true`
4. For `EntityState.Modified` entities (EntityExtra): sets `LastModifiedAtUtc`, `LastModifiedUserId`
5. `OnBeforeSaveChanges()`: captures all change tracker entries, builds AuditEntry objects
6. Persists both data and audit records in same transaction
7. `OnAfterSaveChanges()`: handles temporary properties (database-generated IDs)

#### AuditableIdentityDbContext

Same audit capabilities but extends `IdentityDbContext<TUser, TRole, TKey>` for ASP.NET Identity integration.

### 5.3 Specification Pattern

Specifications encapsulate query logic into reusable, testable objects.

```csharp
// Interface
public interface ISpecification<T> where T : Entity
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>> OrderBy { get; }
    SortDirection SortDirection { get; }
}

// Base class - inherit from this
public abstract class HeroSpecification<T> : ISpecification<T> where T : Entity
{
    protected void AddInclude(Expression<Func<T, object>> includeExpression);
    protected void AddInclude(string includeString);  // For complex paths
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy, 
        SortDirection direction = SortDirection.Descending);
    // Set Criteria directly in constructor
}
```

**Example - Creating a specification:**

```csharp
public class ActiveUsersByRegionSpec : HeroSpecification<User>
{
    public ActiveUsersByRegionSpec(string regionId)
    {
        Criteria = u => u.IsActive && u.RegionId == regionId;
        AddInclude(u => u.Roles);
        AddInclude("Roles.Permissions");  // String-based for deep paths
        ApplyOrderBy(u => u.CreatedAtUtc, SortDirection.Descending);
    }
}

// Usage
var spec = new ActiveUsersByRegionSpec("GH-ACC");
var users = await dbContext.Users.Specify(spec).ToListAsync();
```

**QueryableExtensions:**

```csharp
// Apply specification to IQueryable
IQueryable<T> Specify<T>(this IQueryable<T> query, ISpecification<T> spec)

// Paginate any IQueryable
Task<PaginatedResult<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize)
```

### 5.4 Entity Configurations

All EF Core configurations use `IProjectStringValue` for dynamic schema assignment:

```csharp
public interface IProjectStringValue
{
    string Name { get; }  // Schema name for table organization
}
```

**Standard configurations provided:**

| Configuration | Table Name | Schema | Key Details |
|---------------|------------|--------|-------------|
| `OutboxConfiguration<T>` | `outbox_messages` | Dynamic | Native `Guid` primary key |
| `OutboxMessageConsumerConfiguration<T>` | `outbox_messages_consumer` | Dynamic | Composite key (Id, Name) |
| `AuditTrailConfiguration<T>` | `audits` | Dynamic | Native `Guid` for Id and UserId |
| `LookupTypeConfiguration<T>` | `lookup_types` | Dynamic | One-to-Many with LookupValues, OnDelete: Restrict |
| `LookupValueConfiguration<T>` | `lookup_values` | Dynamic | FK to LookupType |
| `NotificationConfiguration<T>` | `notifications` | Dynamic | NotificationType as string enum |
| `EntityExtraConfiguration<T>` | (reusable static) | N/A | Configures CreatedUserId/LastModifiedUserId as varchar(26) |

**Applying EntityExtra configuration in your own configs:**

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "my_schema");
        builder.HasKey(u => u.Id);
        // Apply shared audit field configuration
        EntityExtraConfiguration<User>.Configure(builder);
        // Your custom configuration...
    }
}
```

### 5.5 Dependency Injection Registration

```csharp
// Register DbContext with PostgreSQL + OutBox interceptor
services.AddDatabaseConfiguration<MyDbContext>(configuration);

// Register Unit of Work
services.AddUnitOfWork<MyDbContext>();

// Register idempotent domain event handling (decorates all handlers automatically)
services.AddOutboxIdempotentConfig<MyDbContext>();

// Register notification repository
services.AddNotificationRepository<MyDbContext>();
```

**Database connection:** Uses `CONNECTION_STRING` from environment variable or `IConfiguration`.

---

## 6. Infrastructure Layer

### 6.1 Caching

**Two implementations of `ICacheManager`:**

| Implementation | Storage | Serialization | Use Case |
|---------------|---------|---------------|----------|
| `DistributedCacheManager` | StackExchange.Redis `IDatabase` | JSON (Newtonsoft.Json) | Multi-instance, production |
| `MemoryCacheManager` | `IMemoryCache` (in-process) | None (direct object) | Single-instance, development |

**Registration:**

```csharp
// Redis distributed cache
services.AddDistributedCacheService(configuration);
// Requires: CACHE_CONNECTION_STRING environment variable or config key

// In-memory cache (development/single instance)
services.AddMemoryCacheService();
```

### 6.2 Background Jobs (Hangfire)

**OutBoxMessagesProcessingJob<T>:**
- Generic over DbContext type
- Queries unprocessed OutboxMessages matching current assembly
- Batch size: 20 messages per execution
- Polly retry: 3 attempts with 50ms exponential backoff
- Tracks: ProcessingAttempts, ProcessLastAttemptOnUtc, Error

**Registration & Dashboard:**

```csharp
// Register Hangfire with Redis storage + OutBox processor
services.AddHangfireConfiguration<MyDbContext>(configuration, prefix: "myservice:");

// Configure dashboard (in middleware pipeline)
app.UseHangfireDashboard("My Service Jobs", "my-service");
// Dashboard URL: /{serviceName}/workers
```

**Hangfire Redis settings:**
- SucceededListSize: 10,000
- DeletedListSize: 1,000
- InvisibilityTimeout: 30 seconds
- ExpiryCheckInterval: 30 seconds

### 6.3 Messaging (MassTransit + RabbitMQ)

All notification services publish integration events via MassTransit:

| Service | Published Event | Pattern |
|---------|----------------|---------|
| `EmailNotificationService` | `EmailNotificationIntegratedEvent` | Pub/Sub via MassTransit |
| `SMSNotificationService` | `SmsNotificationIntegratedEvent` | Pub/Sub + Hangfire background |
| `PushNotificationService` | `PushNotificationIntegratedEvent` | Pub/Sub via MassTransit |

**Registration:**

```csharp
services.AddSharedKernelExternalServices();
// Registers: ICurrentUserService, IEmailNotificationService, ISMSNotificationService, IPushNotificationService
```

### 6.4 Resilience (Polly)

```csharp
public class PollyPolicy<T> where T : class
{
    public static AsyncRetryPolicy Retry(ILogger<T> logger, string failDesc)
    // 3 retries, exponential backoff: 50ms * attempt, logs exceptions
}

// Usage
var policy = PollyPolicy<MyService>.Retry(_logger, "Failed to process payment");
await policy.ExecuteAsync(async () => { /* operation */ });
```

### 6.5 AWS S3 File Storage

```csharp
services.AddAmazonStorageService();
// Requires environment variables:
//   AMAZON_S3_SETTINGS_BUCKET_NAME
//   AMAZON_S3_SETTINGS_ACCESS_KEY_ID
//   AMAZON_S3_SETTINGS_SECRET_ACCESS_KEY
// Region: eu-west-2 (Ireland)
```

### 6.6 JWT Authentication

```csharp
services.AddJwtAuthenticationConfiguration();
// Requires: JWT_ISSUER_SIGNING_KEY environment variable
```

**Configuration:**
- Symmetric key validation
- Clock skew: 0 seconds (strict)
- Issuer/Audience validation: disabled
- Role claim: `ClaimTypes.Role`

**Error responses (JSON):**
- `401` Expired token: `Error("expired_token", "Token has expired...")`
- `401` Invalid token: `Error("unauthorized", "You are not authorized...")`
- `403` Forbidden: `Error("unauthorized_access", "You are not authorized to access...")`

### 6.7 OpenTelemetry (Full Observability Stack)

```csharp
services.AddOpenTelemetryConfiguration(
    services, builder,
    serviceName: "my-service",
    serviceVersion: "1.0.0",
    serviceNamespace: "keed-digital",
    samplingRatio: 1.0  // 0.0 to 1.0
);
```

**Instrumentation sources configured:**

| Category | Instruments |
|----------|-------------|
| **HTTP** | AspNetCore, HttpClient |
| **Data** | Entity Framework Core, SQL Client, StackExchange.Redis |
| **Messaging** | MassTransit |
| **Jobs** | Hangfire |
| **RPC** | gRPC Client |
| **Runtime** | Process metrics, Runtime metrics |
| **Custom** | SharedKernelTelemetry ActivitySource + Meter |

**Export:** All traces, metrics, and logs exported via OTLP (OpenTelemetry Protocol).

### 6.8 gRPC Support

```csharp
// Server-side
services.AddGRPCServerConfiguration();
app.AddGRPCClientServiceConfiguration<MyGrpcService>();

// Client-side
services.AddGrpcClientConfiguration<IMyGrpcService>("MY_SERVICE_ENDPOINT");
// Reads endpoint from environment variable
```

### 6.9 Sentry Error Tracking

```csharp
services.AddSentryConfiguration(configuration);
// Requires: SENTRY_DSN environment variable or config key
// Integrates with OpenTelemetry
```

### 6.10 SMTP Configuration

```csharp
services.AddSMTPServerConfiguration(configuration, "EmailConfiguration");
// Binds to EmailSettings: SmtpHostName, SmtpPort, SmtpUsername, SmtpPassword,
//   SenderEmail, SenderName, UseSsl, DeliveryDecision
```

---

## 7. Presentation Layer

### 7.1 Base API Controller

```csharp
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController<T> : ControllerBase where T : class
{
    // Lazy-loaded from DI container
    protected IMediator Mediator => _mediatorInstance ??= HttpContext.RequestServices.GetService<IMediator>()!;
    protected ILogger<T> Logger => _loggerInstance ??= HttpContext.RequestServices.GetService<ILogger<T>>()!;
}
```

**Usage:**

```csharp
public class UsersController : BaseApiController<UsersController>
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var result = await Mediator.Send(new CreateUserCommand(request.Email, request.FirstName));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }
}
```

### 7.2 Minimal APIs

```csharp
// Define endpoint
public class GetUsersEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetUsersQuery(1, 10));
            return Results.Ok(result);
        });
    }
}

// Register in Program.cs
services.AddEndpoints(typeof(Program).Assembly);          // Discover endpoints
app.AddMinimalApiVersioningSet([(1, 0), (2, 0)]);         // Version + map endpoints

// Or without versioning
app.MapEndpoints();
```

### 7.3 API Versioning

```csharp
// Controller-based versioning
services.AddVersioning(version: 1, versionPrec: 0);

// Minimal API versioning
services.AddMinimalApiVersioning(version: 1, versionPrec: 0);

// URL pattern: /api/v1/[controller] or /api/v{version:apiVersion}/endpoint
// Reports: api-supported-versions and api-deprecated-versions headers
// Default version assumed when client doesn't specify
```

### 7.4 Global Exception Handler

```csharp
services.AddGlobalExceptionHandlerConfig();
app.UseExceptionHandler();  // Must be called in middleware pipeline
```

**Behavior:**
- Catches all unhandled exceptions
- Logs via `ILogger<GlobalExceptionHandler>`
- Sends to Sentry via `SentrySdk.CaptureException()`
- Returns RFC 7807 `ProblemDetails` with status 500
- **Does NOT expose exception details** to clients (security)

### 7.5 Rate Limiting

```csharp
services.AddRateLimitConfig(configuration);
app.UseRateLimiter();  // In middleware pipeline
```

**Configuration (environment variables with defaults):**
- `RATE_LIMIT_PERMIT_LIMIT`: Requests per window (default: 100)
- `RATE_LIMIT_WINDOW_SECONDS`: Window duration (default: 60)
- `RATE_LIMIT_QUEUE_LIMIT`: Queued excess requests (default: 0)
- **Partition key:** Client IP address
- **Algorithm:** Fixed window with auto-replenishment
- **Response on limit:** HTTP 429 with `{ error, retryAfter }` JSON

### 7.6 Swagger / OpenAPI

```csharp
services.AddSwaggerGenConfig("Baobab", "My Service");
app.UseSwaggerUIConfig("Baobab", "My Service");
```

**Features:**
- Bearer JWT security scheme configured
- Security requirements applied to all endpoints
- Deep linking enabled
- Version format: `'v'V` (e.g., "v1")

### 7.7 Structured Logging (Serilog)

```csharp
builder.AddSerilogConfiguration(configuration);
```

**Configuration:**
- Reads from `appsettings.json` via `ReadFrom.Configuration()`
- Enriches with distributed tracing context (`.Enrich.WithSpan()`)
- Optional Seq sink: reads `SEQ_URL` from config or environment variable
- Uses `builder.Host.UseSerilog()` for host-level logging

---

## 8. Building a New Service (Step-by-Step)

### 8.1 Project Setup

Create 4 projects for your service following Clean Architecture:

```
MyService.Domain/          -->  References: SharedKernel.Domain
MyService.Application/     -->  References: SharedKernel.Application, MyService.Domain
MyService.Infrastructure/  -->  References: SharedKernel.Infrastructure, MyService.Application
MyService.Api/             -->  References: SharedKernel.Presentation, MyService.Infrastructure
```

### 8.2 DbContext Setup

```csharp
// Inherit from AuditableContext for automatic audit trail
public class MyServiceDbContext : AuditableContext<MyServiceDbContext>
{
    public DbSet<Order> Orders { get; set; }
    // DbSet<Audit> AuditTrail inherited from AuditableContext
    // DbSet<OutboxMessage> inherited via interceptor

    public MyServiceDbContext(
        DbContextOptions<MyServiceDbContext> options,
        ICurrentUserService currentUserService,
        ILogger<MyServiceDbContext> logger) : base(options, currentUserService, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var project = new MyProjectSchema();  // Implements IProjectStringValue
        
        // Apply SharedKernel configurations
        modelBuilder.ApplyConfiguration(new OutboxConfiguration<MyProjectSchema>(project));
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration<MyProjectSchema>(project));
        modelBuilder.ApplyConfiguration(new AuditTrailConfiguration<MyProjectSchema>(project));
        modelBuilder.ApplyConfiguration(new NotificationConfiguration<MyProjectSchema>(project));
        modelBuilder.ApplyConfiguration(new LookupTypeConfiguration<MyProjectSchema>(project));
        modelBuilder.ApplyConfiguration(new LookupValueConfiguration<MyProjectSchema>(project));

        // Apply your own configurations
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
    }
}

// Schema definition
public class MyProjectSchema : IProjectStringValue
{
    public string Name => "my_service";  // Database schema name
}
```

### 8.3 Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---

// SharedKernel services
builder.Services.AddMediatorConfig<Program>();                                    // MediatR + behaviors
builder.Services.AddAssemblyValidator<Program>();                                 // FluentValidation
builder.Services.AddDatabaseConfiguration<MyServiceDbContext>(configuration);     // EF Core + PostgreSQL
builder.Services.AddUnitOfWork<MyServiceDbContext>();                             // Unit of Work
builder.Services.AddOutboxIdempotentConfig<MyServiceDbContext>();                 // Idempotent event handling
builder.Services.AddSharedKernelExternalServices();                               // User context + notifications
builder.Services.AddNotificationRepository<MyServiceDbContext>();                 // Notification persistence
builder.Services.AddMassTransitRabbitMQConfig<Program>(builder.Configuration);    // MassTransit + RabbitMQ
builder.Services.AddDistributedCacheService(builder.Configuration);               // Redis cache
builder.Services.AddJwtAuthenticationConfiguration();                             // JWT auth
builder.Services.AddHangfireConfiguration<MyServiceDbContext>(builder.Configuration, "myservice:");
builder.Services.AddOpenTelemetryConfiguration(builder.Services, builder, "my-service", "1.0.0");
builder.Services.AddSentryConfiguration(builder.Configuration);
builder.Services.AddAmazonStorageService();                                       // AWS S3

// Presentation
builder.Services.AddVersioning(1, 0);
builder.Services.AddSwaggerGenConfig("Baobab", "My Service");
builder.Services.AddGlobalExceptionHandlerConfig();
builder.Services.AddRateLimitConfig(builder.Configuration);
builder.AddSerilogConfiguration(builder.Configuration);

// Standard ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// --- Middleware Pipeline ---

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUIConfig("Baobab", "My Service");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard("My Service Jobs", "my-service");

// Schedule OutBox processing
RecurringJob.AddOrUpdate<IOutBoxMessagesProcessingJob>(
    "outbox-processor",
    job => job.Execute(CancellationToken.None),
    "*/10 * * * * *");  // Every 10 seconds

app.Run();
```

### 8.4 Define Your Domain

```csharp
// Aggregate Root
public class Order : AggregateRoot
{
    public Guid Id { get; private set; }
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    private Order() { }  // EF Core constructor

    public static Order Create(Money amount)
    {
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            TotalAmount = amount,
            Status = OrderStatus.Pending
        };
        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id));
        return order;
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Fail(new Error("invalid_status", "Only pending orders can be confirmed"));

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderConfirmedDomainEvent(Id));
        return Result.Success();
    }
}

// Domain event
public sealed record OrderCreatedDomainEvent(Guid OrderId) : DomainEvent(Guid.CreateVersion7());

// Domain event handler
public class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly IEmailNotificationService _emailService;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _emailService.PublishNotificationAsync(
            "customer@example.com", "Order Created",
            $"Your order {notification.OrderId} has been created.", "MyProduct");
    }
}
```

### 8.5 Define Commands & Queries

```csharp
// Command
public record CreateOrderCommand(string Currency, decimal Amount) : ICommand<Guid>;

// Validator
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

// Handler
public class CreateOrderCommandHandler(MyServiceDbContext dbContext)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<IResult<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var moneyResult = Money.Validate(request.Currency, request.Amount);
        if (!moneyResult.Succeeded) return ResultT<Guid>.Fail(moneyResult.Messages);

        var order = Order.Create(Money.Create(request.Currency, request.Amount));
        await dbContext.Orders.AddAsync(order, ct);
        // SaveChanges handled by UnitOfWorkPipelineBehavior
        // OutBox interceptor captures OrderCreatedDomainEvent

        return ResultT<Guid>.Success(order.Id);
    }
}
```

### 8.6 Define API Controller

```csharp
[ApiVersion("1.0")]
public class OrdersController : BaseApiController<OrdersController>
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var result = await Mediator.Send(
            new CreateOrderCommand(request.Currency, request.Amount));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }
}
```

---

## 9. Code Standards & Conventions

### 9.1 Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Commands | `{Verb}{Entity}Command` | `CreateOrderCommand` |
| Queries | `Get{Entity}Query`, `Get{Entity}ByIdQuery` | `GetOrdersQuery` |
| Command Handlers | `{Command}Handler` | `CreateOrderCommandHandler` |
| Query Handlers | `{Query}Handler` | `GetOrderByIdQueryHandler` |
| Validators | `{Command}Validator` | `CreateOrderCommandValidator` |
| Domain Events | `{Entity}{Action}DomainEvent` | `OrderCreatedDomainEvent` |
| Event Handlers | `{Event}Handler` | `OrderCreatedDomainEventHandler` |
| Specifications | `{Description}Spec` | `ActiveUsersByRegionSpec` |
| Error Codes | `snake_case` | `"email_address_invalid"` |
| DB Tables | `snake_case` (via NamingConventions) | `outbox_messages`, `lookup_types` |
| DB Columns | `snake_case` (automatic) | `created_at_utc`, `is_active` |

### 9.2 Error Handling Rules

1. **Never throw exceptions** for business logic failures - use the Result pattern
2. **Validate value objects** before creating them (call `Validate()` then `Create()`)
3. **Use centralized error records** in `Errors.cs` with snake_case codes
4. **Return typed results** from all handlers (`IResult`, `IResult<T>`, `PaginatedResult<T>`)
5. **Let the ValidationPipelineBehavior** handle FluentValidation failures automatically
6. **Exceptions are only for truly exceptional situations** (infrastructure failures, etc.)

### 9.3 Domain Modeling Rules

1. **Aggregate roots** manage their own domain events via `RaiseDomainEvent()`
2. **Value objects** are immutable - all properties have private setters
3. **Entities inherit from EntityExtra** for automatic audit fields
4. **Use factory methods** (`Create()`) instead of public constructors
5. **Private parameterless constructors** for EF Core compatibility
6. **Domain events are raised inside the aggregate**, not from handlers
7. **Use `Guid.CreateVersion7()`** for entity identifiers (sortable, distributed-safe) — never `Guid.NewGuid()`

### 9.4 CQRS Rules

1. **Commands modify state**, queries only read
2. **All commands go through the pipeline**: Logging -> UnitOfWork -> Validation -> Handler
3. **Validators are automatically discovered** from the assembly
4. **UnitOfWork handles SaveChanges** - don't call it in handlers unless needed
5. **One handler per command/query** - no shared handlers
6. **Use `IPaginatedQuery`** for list endpoints with pagination

### 9.5 Database Rules

1. **PostgreSQL** with `snake_case` naming convention (automatic via EFCore.NamingConventions)
2. **Guid stored natively** (`uuid`/`uniqueidentifier`, no string conversion) in all primary keys and foreign keys
3. **Use `IProjectStringValue`** for schema isolation per service
4. **Connection string** from `CONNECTION_STRING` environment variable
5. **Apply `EntityExtraConfiguration`** on all entities inheriting EntityExtra
6. **Schema-per-service** to isolate tables when sharing databases

### 9.6 API Design Rules

1. **Inherit from `BaseApiController<T>`** for automatic auth, MediatR, and logging
2. **Use API versioning** in all endpoints (`api/v{version}/[controller]`)
3. **Return `IActionResult`** from controllers, mapping Result to HTTP status codes
4. **`[Authorize]` is inherited** from base controller - use `[AllowAnonymous]` to opt out
5. **Use `ProblemDetails`** for error responses (handled by GlobalExceptionHandler)

---

## 10. Environment Variables & Configuration Reference

### Required Variables

| Variable | Layer | Purpose | Example |
|----------|-------|---------|---------|
| `CONNECTION_STRING` | Persistence | PostgreSQL connection string | `Host=localhost;Database=mydb;Username=postgres;Password=pass` |
| `JWT_ISSUER_SIGNING_KEY` | Infrastructure | JWT token signing key | `my-256-bit-secret-key-here` |

### Messaging (MassTransit + RabbitMQ)

| Variable | Default | Purpose |
|----------|---------|---------|
| `SERVICE_BUS_URI` | `http://localhost:5672` | RabbitMQ connection URI |
| `SERVICE_BUS_USER_NAME` | `guest` | RabbitMQ username |
| `SERVICE_BUS_PASSWORD` | `guest` | RabbitMQ password |

### Caching (Redis)

| Variable | Default | Purpose |
|----------|---------|---------|
| `CACHE_CONNECTION_STRING` | (required) | Redis connection string |

### AWS S3

| Variable | Purpose |
|----------|---------|
| `AMAZON_S3_SETTINGS_BUCKET_NAME` | S3 bucket name |
| `AMAZON_S3_SETTINGS_ACCESS_KEY_ID` | AWS access key |
| `AMAZON_S3_SETTINGS_SECRET_ACCESS_KEY` | AWS secret key |

### API Keys

| Variable | Purpose |
|----------|---------|
| `API_KEY` | API key prefix for validation |
| `API_SECRET` | API secret for key generation and validation |

### Observability

| Variable | Purpose |
|----------|---------|
| `SEQ_URL` | Seq log aggregation endpoint |
| `SENTRY_DSN` | Sentry error tracking DSN |
| `OTEL_SERVICE_INSTANCE_ID` | OpenTelemetry instance identifier |

### Audit System

| Variable | Purpose |
|----------|---------|
| `SYSTEM_ADMIN_ID` | Guid for audit trail when no user context (background jobs) |

### Rate Limiting

| Variable | Default | Purpose |
|----------|---------|---------|
| `RATE_LIMIT_PERMIT_LIMIT` | `100` | Requests per window |
| `RATE_LIMIT_WINDOW_SECONDS` | `60` | Window duration in seconds |
| `RATE_LIMIT_QUEUE_LIMIT` | `0` | Queue size for excess requests |

---

## 11. Technology Stack & NuGet Packages

### Core Framework

| Package | Version | Purpose |
|---------|---------|---------|
| .NET | 10.0 | Target framework |
| MediatR | 12.5.0 | CQRS message bus |
| MediatR.Contracts | 2.0.1 | Shared contracts |
| FluentValidation | 12.1.1 | Request validation |
| Newtonsoft.Json | 13.0.4 | JSON serialization |

### Data Access

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 10.0.8 | ORM framework |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 | PostgreSQL provider |
| EFCore.NamingConventions | 10.0.1 | snake_case conventions |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.8 | ASP.NET Identity |

### Messaging

| Package | Version | Purpose |
|---------|---------|---------|
| MassTransit | 8.5.9 | Message bus abstraction |
| MassTransit.RabbitMQ | 8.5.9 | RabbitMQ transport |

### Caching

| Package | Version | Purpose |
|---------|---------|---------|
| StackExchange.Redis | 2.13.17 | Redis client |

### Background Jobs

| Package | Version | Purpose |
|---------|---------|---------|
| Hangfire.Core | 1.8.23 | Job processing |
| Hangfire.AspNetCore | 1.8.23 | ASP.NET integration |
| Hangfire.Redis.StackExchange | 1.12.0 | Redis storage |

### Resilience

| Package | Version | Purpose |
|---------|---------|---------|
| Polly | 8.6.6 | Retry and fault handling |

### Observability

| Package | Version | Purpose |
|---------|---------|---------|
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.3 | OTLP exporter |
| OpenTelemetry.Extensions.Hosting | 1.15.3 | Hosting integration |
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.2 | HTTP tracing |
| OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.15.1-beta.1 | EF Core tracing |
| OpenTelemetry.Instrumentation.Http | 1.15.1 | HTTP client tracing |
| OpenTelemetry.Instrumentation.SqlClient | 1.15.2 | SQL tracing |
| OpenTelemetry.Instrumentation.StackExchangeRedis | 1.15.1-beta.2 | Redis tracing |
| OpenTelemetry.Instrumentation.Hangfire | 1.15.1-beta.1 | Job tracing |
| OpenTelemetry.Instrumentation.GrpcNetClient | 1.15.1-beta.1 | gRPC tracing |
| OpenTelemetry.Instrumentation.Process | 1.15.1-beta.1 | Process metrics |
| OpenTelemetry.Instrumentation.Runtime | 1.15.1 | Runtime metrics |
| Sentry | 6.6.0 | Error tracking |
| Sentry.OpenTelemetry | 6.6.0 | Sentry + OTEL integration |
| Serilog.AspNetCore | 10.0.0 | Structured logging |
| Serilog.Enrichers.Span | 3.1.0 | Trace context enrichment |
| Serilog.Sinks.Seq | 9.1.0 | Seq log sink |

### API & Protocol

| Package | Version | Purpose |
|---------|---------|---------|
| Asp.Versioning.Http | 10.0.0 | HTTP versioning |
| Asp.Versioning.Mvc | 10.0.0 | MVC versioning |
| Asp.Versioning.Mvc.ApiExplorer | 10.0.0 | Swagger versioning |
| Swashbuckle.AspNetCore | 10.2.1 | Swagger generation |
| Grpc.AspNetCore | 2.80.0 | gRPC server |
| Grpc.Net.Client | 2.80.0 | gRPC client |
| Google.Protobuf | 3.35.0 | Protocol Buffers |

### Security

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.8 | JWT authentication |

### Cloud Services

| Package | Version | Purpose |
|---------|---------|---------|
| AWSSDK.S3 | 4.0.24 | AWS S3 file storage |

---

## Quick Reference Card

```
COMMAND FLOW:
  Controller -> Mediator.Send(command) -> Logging -> UnitOfWork -> Validation -> Handler -> SaveChanges

QUERY FLOW:
  Controller -> Mediator.Send(query) -> Logging -> (skip UnitOfWork) -> Validation -> Handler

EVENT FLOW:
  Handler -> AggregateRoot.RaiseDomainEvent() -> SaveChanges Interceptor -> OutboxMessage table
  -> Hangfire OutBoxProcessingJob -> EventFactory -> MediatR Publish -> IdempotentHandler -> EventHandler

ERROR FLOW:
  Validation failure -> ValidationPipelineBehavior -> ValidationResult.WithErrors()
  Business failure -> Handler -> Result.Fail(Error) -> Controller -> BadRequest/NotFound
  Unhandled exception -> GlobalExceptionHandler -> Sentry + ProblemDetails (500)

AUDIT FLOW:
  Any entity change -> AuditableContext.SaveChangesAsync() -> OnBeforeSaveChanges()
  -> Audit entity created -> Both saved in same transaction
```

---

*Generated from source code analysis of the Baobab SharedKernel repository. For the latest information, always refer to the actual codebase.*
