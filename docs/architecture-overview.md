# Architecture Overview

## Understanding the Baobab SharedKernel Architecture

The Baobab SharedKernel implements a sophisticated **Clean Architecture** pattern combined with **Domain-Driven Design (DDD)** principles to create a robust foundation for enterprise microservices. This architecture ensures your applications are maintainable, testable, and scalable from day one.

## 🏗️ The Big Picture

```
┌─────────────────────────────────────────────────────────┐
│                    Microservice                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │                Presentation Layer                   ││
│  │        (API Controllers, Minimal APIs)              ││
│  └─────────────────────────────────────────────────────┘│
│                            │                            │
│  ┌─────────────────────────────────────────────────────┐│
│  │               Application Layer                     ││
│  │       (Commands, Queries, Handlers, DTOs)          ││
│  └─────────────────────────────────────────────────────┘│
│                            │                            │
│  ┌─────────────────────────────────────────────────────┐│
│  │                Infrastructure Layer                 ││
│  │     (External Services, Messaging, Caching)        ││
│  └─────────────────────────────────────────────────────┘│
│                            │                            │
│  ┌─────────────────────────────────────────────────────┐│
│  │               Persistence Layer                     ││
│  │      (Database, Repositories, Configurations)      ││
│  └─────────────────────────────────────────────────────┘│
│                            │                            │
│  ┌─────────────────────────────────────────────────────┐│
│  │                  Domain Layer                       ││
│  │    (Entities, Value Objects, Domain Events)        ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────┐
│               SharedKernel Foundation                   │
│   (Common Abstractions, Patterns, Base Classes)        │
└─────────────────────────────────────────────────────────┘
```

## 🎯 Core Design Principles

### 1. **Dependency Inversion**
Dependencies always point inward. The Domain layer has no dependencies, while outer layers depend on inner layers through abstractions.

### 2. **Separation of Concerns**
Each layer has a single, well-defined responsibility:
- **Domain**: Business logic and rules
- **Application**: Use case orchestration
- **Infrastructure**: External system integration
- **Persistence**: Data storage and retrieval
- **Presentation**: User interface and API endpoints

### 3. **Explicit Architecture**
The architecture makes dependencies, data flow, and responsibilities explicit through interfaces and clear layer boundaries.

### 4. **Testability**
Every component can be unit tested in isolation through dependency injection and interface-based design.

## 📚 Layer-by-Layer Breakdown

### 🏛️ Domain Layer (Core)

**Purpose**: Contains the business logic, domain rules, and core entities.

**Key Components**:
- **Aggregate Roots**: Main business entities that maintain consistency
- **Value Objects**: Immutable objects that encapsulate business values
- **Domain Events**: Capture important business occurrences
- **Domain Services**: Complex business logic that doesn't belong to a single entity
- **Repository Interfaces**: Contracts for data access

**Dependencies**: None (Pure business logic)

```csharp
// Example: User Aggregate Root
public class User : AggregateRoot
{
    public UserId Id { get; private set; }
    public EmailAddress Email { get; private set; }
    
    public void ChangeEmail(EmailAddress newEmail)
    {
        if (Email == newEmail) return;
        
        Email = newEmail;
        RaiseDomainEvent(new UserEmailChangedDomainEvent(Id, Email));
    }
}
```

### 🔄 Application Layer (Use Cases)

**Purpose**: Orchestrates domain objects to fulfill application use cases.

**Key Components**:
- **Commands**: Operations that change system state
- **Queries**: Read operations that return data
- **Handlers**: Process commands and queries
- **DTOs**: Data transfer objects for external communication
- **Validators**: Input validation using FluentValidation
- **Pipeline Behaviors**: Cross-cutting concerns (logging, validation, transactions)

**Dependencies**: Domain Layer only

```csharp
// Example: Command Handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Orchestrate domain objects
        var user = new User(/* ... */);
        _repository.Add(user);
        await _unitOfWork.SaveChangesAsync();
        return Result<UserId>.Success(user.Id);
    }
}
```

### 🔧 Infrastructure Layer (External World)

**Purpose**: Implements external service integrations and cross-cutting concerns.

**Key Components**:
- **External Service Clients**: APIs, messaging systems, file storage
- **Caching**: Redis distributed cache and memory cache
- **Background Jobs**: Hangfire job processing
- **Messaging**: MassTransit + RabbitMQ integration
- **Resilience**: Polly retry policies
- **Notifications**: Email and SMS services

**Dependencies**: Application and Domain layers

```csharp
// Example: External service implementation
public class EmailNotificationService : IEmailNotificationService
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        // Integration with external email provider
        await _emailProvider.SendAsync(message, cancellationToken);
    }
}
```

### 🗄️ Persistence Layer (Data Access)

**Purpose**: Handles data persistence and retrieval using Entity Framework Core.

**Key Components**:
- **DbContext**: Database context with auditing support
- **Repository Implementations**: Concrete data access implementations
- **Entity Configurations**: EF Core entity mappings
- **Migrations**: Database schema versioning
- **OutBox Pattern**: Reliable event publishing
- **Unit of Work**: Transaction management

**Dependencies**: Domain layer, some Application abstractions

```csharp
// Example: Repository implementation
public class UserRepository : IUserRepository
{
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
```

### 🌐 Presentation Layer (API)

**Purpose**: Exposes application functionality through REST APIs.

**Key Components**:
- **API Controllers**: RESTful endpoint implementations
- **Minimal APIs**: Lightweight endpoint definitions
- **Global Exception Handler**: Centralized error handling
- **API Versioning**: Version management
- **Authentication/Authorization**: Security implementation

**Dependencies**: Application layer only

```csharp
// Example: API Controller
[ApiVersion("1.0")]
public class UsersController : BaseApiController<UsersController>
{
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserCommand command)
    {
        var result = await Mediator.Send(command);
        return result.Succeeded ? Ok(result.Value) : BadRequest(result.Messages);
    }
}
```

## 🔄 Data Flow and Communication Patterns

### 1. **Request Flow** (Inbound)
```
HTTP Request → Controller → MediatR → Command/Query Handler → Domain Logic → Repository → Database
```

### 2. **Event Flow** (Outbound)
```
Domain Event → OutBox → Background Job → Message Bus → Other Services
```

### 3. **Cross-Cutting Concerns**
```
Pipeline Behaviors: Validation → Logging → Unit of Work → Handler
```

## 🚀 Key Architectural Patterns

### CQRS (Command Query Responsibility Segregation)

**Commands**: Change system state, return simple results
```csharp
public record CreateUserCommand(string Email, string FirstName) : ICommand<Result<UserId>>;
```

**Queries**: Read data, return DTOs
```csharp
public record GetUserQuery(UserId Id) : IQuery<Result<UserDto>>;
```

### Result Pattern

Explicit success/failure handling without exceptions:
```csharp
public async Task<Result<User>> CreateUser(CreateUserRequest request)
{
    if (await _repository.ExistsAsync(request.Email))
        return Result<User>.Fail(Error.Conflict("User already exists"));
        
    var user = new User(request.Email, request.FirstName);
    return Result<User>.Success(user);
}
```

### Domain Events

Decouple business logic through events:
```csharp
// Raise in aggregate
RaiseDomainEvent(new UserCreatedDomainEvent(Id, Email));

// Handle in separate handler
public class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Send welcome email, update analytics, etc.
    }
}
```

### OutBox Pattern

Reliable event publishing in distributed systems:
```csharp
// Events are stored in database transaction
public class OutboxMessage
{
    public string Type { get; set; }
    public string Content { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedDateUtc { get; set; }
}
```

## 🏢 Microservice Communication

### Synchronous Communication
- **HTTP APIs**: Direct service-to-service calls
- **API Gateway**: Single entry point (YARP integration)
- **Service Discovery**: Dynamic service location

### Asynchronous Communication
- **Domain Events**: Internal service events
- **Integration Events**: Cross-service events
- **Message Bus**: RabbitMQ + MassTransit

### Event-Driven Architecture

```
┌─────────────┐    Domain     ┌─────────────┐    Integration    ┌─────────────┐
│   Service   │    Events     │   OutBox    │     Events       │   Message   │
│     A       │──────────────▶│  Processor  │─────────────────▶│     Bus     │
└─────────────┘               └─────────────┘                  └─────────────┘
                                                                       │
┌─────────────┐    Integration Events                                  │
│   Service   │◀───────────────────────────────────────────────────────┘
│     B       │
└─────────────┘
```

## 🛡️ Cross-Cutting Concerns

### Security
- **Authentication**: JWT token validation
- **Authorization**: Role and policy-based access
- **API Keys**: Service-to-service authentication
- **Input Validation**: FluentValidation integration

### Observability
- **Logging**: Structured logging with Serilog
- **Metrics**: Application performance metrics
- **Tracing**: Distributed tracing support
- **Health Checks**: Service health monitoring

### Resilience
- **Retry Policies**: Automatic retry with Polly
- **Circuit Breakers**: Prevent cascade failures
- **Timeouts**: Request timeout handling
- **Bulkhead**: Resource isolation

### Performance
- **Caching**: Multi-level caching strategy
- **Connection Pooling**: Database connection management
- **Pagination**: Efficient data retrieval
- **Lazy Loading**: On-demand data loading

## 🎯 Benefits of This Architecture

### ✅ Maintainability
- **Clear Boundaries**: Each layer has well-defined responsibilities
- **Loose Coupling**: Changes in one layer don't affect others
- **High Cohesion**: Related functionality is grouped together

### ✅ Testability
- **Unit Testing**: Each component can be tested in isolation
- **Integration Testing**: Test layer interactions
- **Behavior Testing**: Verify business requirements

### ✅ Scalability
- **Horizontal Scaling**: Add more service instances
- **Vertical Scaling**: Optimize individual components
- **Database Scaling**: Read replicas and sharding support

### ✅ Flexibility
- **Technology Agnostic**: Swap out implementations easily
- **Framework Independence**: Not tied to specific frameworks
- **Cloud Ready**: Deploy anywhere (Docker, Kubernetes, Cloud)

### ✅ Developer Experience
- **Consistent Patterns**: Same patterns across all services
- **Code Generation**: Reduce boilerplate with templates
- **IntelliSense**: Strong typing throughout

## 🚀 Next Steps

Now that you understand the architecture, dive deeper into specific areas:

1. **[Getting Started](./getting-started.md)** - Build your first service
2. **[Team Architecture Handoff Guide](./team-architecture-handoff-guide.md)** - Layer-by-layer reference (Domain, Application, Persistence, Infrastructure, Presentation)
3. **[Patterns and Best Practices](./patterns-and-practices.md)** - Proven patterns with real examples

Ready to see this in action? Check out our [Practical Examples](./examples.md)!