# Troubleshooting Guide

## Common Issues and Solutions

This guide addresses the most common issues developers encounter when working with the Baobab SharedKernel architecture, along with step-by-step solutions and best practices for prevention.

## 🏗️ Architecture and Setup Issues

### Issue 1: "The type or namespace name 'Baobab' could not be found"

**Symptoms:**
- Compilation errors when referencing SharedKernel packages
- IntelliSense not recognizing SharedKernel types

**Root Cause:**
Missing or incorrect package references in your project files.

**Solution:**
```xml
<!-- Ensure your .csproj files have the correct references -->
<ItemGroup>
  <ProjectReference Include="..\..\SharedKernel\Baobab.SharedKernel.Domain\Baobab.SharedKernel.Domain.csproj" />
  <ProjectReference Include="..\..\SharedKernel\Baobab.SharedKernel.Application\Baobab.SharedKernel.Application.csproj" />
  <!-- Add other SharedKernel references as needed -->
</ItemGroup>
```

**Prevention:**
- Use a Directory.Build.props file to manage common references
- Create project templates with the correct references

---

### Issue 2: "Unable to resolve service for type 'IMediator'"

**Symptoms:**
- Runtime dependency injection errors
- Controllers failing to resolve MediatR

**Root Cause:**
MediatR not properly registered in the DI container.

**Solution:**
```csharp
// In Program.cs or Startup.cs
builder.Services.AddMediatorConfig<Program>(); // This registers MediatR with behaviors

// If you need custom configuration:
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
    config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
    config.AddOpenBehavior(typeof(UnitOfWorkPipelineBehavior<,>));
});
```

**Prevention:**
- Always use the SharedKernel extension methods for service registration
- Create integration tests that verify DI container configuration

---

## 🗄️ Database and Persistence Issues

### Issue 3: "The entity type 'User' requires a primary key to be defined"

**Symptoms:**
- EF Core migration errors
- Database context configuration failures

**Root Cause:**
Entity configurations missing or incorrect ID mapping.

**Solution:**
```csharp
// Entity Configuration
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        // Configure other properties...
    }
}

// Ensure configuration is applied in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
```

**Prevention:**
- Always create entity configurations for your aggregates
- Use the SharedKernel base classes (Entity, AggregateRoot) consistently

---

### Issue 4: "Value cannot be null. Parameter name: connectionString"

**Symptoms:**
- Application startup failures
- Database connection errors

**Root Cause:**
Database connection string not configured or incorrectly named.

**Solution:**
```csharp
// In appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydb;Username=postgres;Password=postgres"
  }
}

// In Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Alternative for development:**
```csharp
// Use environment variable
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not found");
```

**Prevention:**
- Use user secrets for development
- Implement startup health checks for database connectivity

---

### Issue 5: "Domain events not being published"

**Symptoms:**
- Event handlers not executing
- OutBox messages not being created

**Root Cause:**
Domain event interceptor not configured or UnitOfWork not saving changes.

**Solution:**
```csharp
// Ensure ConvertDomainEventsToOutboxMessagesInterceptor is registered
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new ConvertDomainEventsToOutboxMessagesInterceptor());
}

// Or in DI registration:
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(connectionString)
           .AddInterceptors(serviceProvider.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
});

// Ensure UnitOfWork is saving changes
public class SomeCommandHandler : ICommandHandler<SomeCommand, Result>
{
    public async Task<Result> Handle(SomeCommand request, CancellationToken cancellationToken)
    {
        // Domain logic that raises events
        var aggregate = new SomeAggregate();
        aggregate.DoSomething(); // This raises domain events
        
        _repository.Add(aggregate);
        
        // This is crucial - without SaveChangesAsync, events won't be processed
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
```

**Prevention:**
- Always call SaveChangesAsync in command handlers
- Use the UnitOfWorkPipelineBehavior to automatically save changes

---

## 🔧 Application Logic Issues

### Issue 6: "FluentValidation validators not being called"

**Symptoms:**
- Invalid data passing through to handlers
- Validation errors not returned

**Root Cause:**
Validators not registered or ValidationPipelineBehavior not configured.

**Solution:**
```csharp
// Register validators
builder.Services.AddAssemblyValidator<Program>();

// Ensure ValidationPipelineBehavior is registered (included in AddMediatorConfig)
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
    config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>)); // This must be registered
});

// Validator must follow naming convention
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
    }
}
```

**Prevention:**
- Use consistent naming conventions for validators
- Create base test classes that verify validator registration

---

### Issue 7: "Value object creation failing silently"

**Symptoms:**
- Null value objects in domain entities
- Unexpected behavior in business logic

**Root Cause:**
Not checking Result<T> return values from value object creation.

**Solution:**
```csharp
// ❌ Wrong - ignoring result
var email = EmailAddress.Create(request.Email).Value; // Could be null!

// ✅ Correct - checking result
var emailResult = EmailAddress.Create(request.Email);
if (!emailResult.Succeeded)
    return Result<User>.Fail(emailResult.Messages);

var email = emailResult.Value!; // Safe to use

// Or use a helper extension method
public static class ResultExtensions
{
    public static T ValueOrThrow<T>(this Result<T> result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Result failed: {string.Join(", ", result.Messages.Select(e => e.Message))}");
        
        return result.Value!;
    }
}

// Usage
var email = EmailAddress.Create(request.Email).ValueOrThrow();
```

**Prevention:**
- Always check Result<T> return values
- Use static analysis tools to detect unused return values
- Create unit tests that verify error conditions

---

## 🌐 API and Integration Issues

### Issue 8: "HTTP 500 errors with no detailed error information"

**Symptoms:**
- Generic error responses
- No useful debugging information

**Root Cause:**
Global exception handler not configured or not catching specific exceptions.

**Solution:**
```csharp
// Ensure GlobalExceptionHandler is registered
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler(); // This must be called

// Custom exception handler
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = validationEx.Message
            },
            BusinessRuleViolationException businessEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", 
                Title = "Business Rule Violation",
                Status = StatusCodes.Status400BadRequest,
                Detail = businessEx.Message
            },
            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "An error occurred",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An internal server error occurred"
            }
        };

        httpContext.Response.StatusCode = response.Status ?? 500;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
```

**Prevention:**
- Implement comprehensive logging
- Use structured logging with correlation IDs
- Create health check endpoints

---

### Issue 9: "API returning 200 OK but with error messages in body"

**Symptoms:**
- Successful HTTP status codes with error content
- Inconsistent error handling

**Root Cause:**
Controllers not properly mapping Result<T> to HTTP status codes.

**Solution:**
```csharp
// ❌ Wrong - always returns 200
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserCommand command)
{
    var result = await Mediator.Send(command);
    return Ok(result); // This always returns 200, even for failures
}

// ✅ Correct - proper status code mapping
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserCommand command)
{
    var result = await Mediator.Send(command);
    
    if (!result.Succeeded)
    {
        // Map errors to appropriate status codes
        var errorType = result.Messages.FirstOrDefault()?.Type;
        return errorType switch
        {
            ErrorType.Validation => BadRequest(result.Messages),
            ErrorType.NotFound => NotFound(result.Messages),
            ErrorType.Conflict => Conflict(result.Messages),
            ErrorType.Unauthorized => Unauthorized(),
            ErrorType.Forbidden => Forbid(),
            _ => Problem(detail: "An error occurred", statusCode: 500)
        };
    }
    
    return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Value }, result.Value);
}

// Or create an extension method
public static class ControllerExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.Succeeded)
            return new OkObjectResult(result.Value);
            
        var errorType = result.Messages.FirstOrDefault()?.Type ?? ErrorType.Failure;
        return errorType switch
        {
            ErrorType.Validation => new BadRequestObjectResult(result.Messages),
            ErrorType.NotFound => new NotFoundObjectResult(result.Messages),
            ErrorType.Conflict => new ConflictObjectResult(result.Messages),
            _ => new ObjectResult(result.Messages) { StatusCode = 500 }
        };
    }
}

// Usage
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserCommand command)
{
    var result = await Mediator.Send(command);
    return result.ToActionResult();
}
```

**Prevention:**
- Use the BaseApiController consistently
- Create extension methods for common Result<T> mappings
- Write integration tests that verify HTTP status codes

---

## 🚀 Performance Issues

### Issue 10: "Slow database queries and N+1 problems"

**Symptoms:**
- High response times
- Excessive database queries
- Poor application performance

**Root Cause:**
Missing eager loading, inefficient queries, or lack of proper indexing.

**Solution:**
```csharp
// ❌ N+1 Problem
public async Task<List<OrderDto>> GetOrdersAsync()
{
    var orders = await _context.Orders.ToListAsync();
    
    // This creates N+1 queries
    return orders.Select(order => new OrderDto
    {
        Id = order.Id,
        CustomerName = order.Customer.Name, // Lazy loading - extra query per order
        Items = order.Items.ToList() // Another query per order
    }).ToList();
}

// ✅ Proper eager loading
public async Task<List<OrderDto>> GetOrdersAsync()
{
    return await _context.Orders
        .Include(o => o.Customer)
        .Include(o => o.Items)
            .ThenInclude(i => i.Product)
        .Select(order => new OrderDto
        {
            Id = order.Id,
            CustomerName = order.Customer.Name,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        })
        .ToListAsync();
}

// For read-heavy operations, use Dapper
public async Task<List<OrderSummaryDto>> GetOrderSummariesAsync()
{
    const string sql = @"
        SELECT 
            o.Id,
            o.CreatedAt,
            o.Total,
            c.FirstName || ' ' || c.LastName AS CustomerName
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerId = c.Id
        ORDER BY o.CreatedAt DESC";

    using var connection = new NpgsqlConnection(_connectionString);
    var results = await connection.QueryAsync<OrderSummaryDto>(sql);
    return results.ToList();
}
```

**Database Indexing:**
```sql
-- Create indexes for common queries
CREATE INDEX IX_Orders_CustomerId ON Orders(CustomerId);
CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);
CREATE INDEX IX_Orders_Status_CreatedAt ON Orders(Status, CreatedAt);

-- For searching
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Products_Name_gin ON Products USING gin(to_tsvector('english', Name));
```

**Prevention:**
- Monitor database query performance
- Use Entity Framework logging to identify slow queries
- Implement caching for read-heavy operations
- Write performance tests for critical paths

---

### Issue 11: "Memory leaks and high memory usage"

**Symptoms:**
- Increasing memory usage over time
- OutOfMemory exceptions
- Poor garbage collection performance

**Root Cause:**
Undisposed resources, large object retention, or inefficient caching.

**Solution:**
```csharp
// ❌ Resource leak
public async Task ProcessLargeFileAsync(string filePath)
{
    var fileStream = new FileStream(filePath, FileMode.Open);
    var reader = new StreamReader(fileStream);
    
    // Process file...
    // FileStream and StreamReader are never disposed!
}

// ✅ Proper resource management
public async Task ProcessLargeFileAsync(string filePath)
{
    await using var fileStream = new FileStream(filePath, FileMode.Open);
    using var reader = new StreamReader(fileStream);
    
    // Process file...
    // Resources are automatically disposed
}

// ❌ Inefficient caching
public class BadCacheService
{
    private readonly Dictionary<string, object> _cache = new();

    public void Set<T>(string key, T value)
    {
        _cache[key] = value; // Never expires - memory leak!
    }
}

// ✅ Proper caching with expiration
public class GoodCacheService
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 1000, // Limit cache size
        CompactionPercentage = 0.1 // Cleanup when limit reached
    });

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Size = 1
        };
        
        _cache.Set(key, value, options);
    }
}
```

**Prevention:**
- Use `using` statements for disposable resources
- Implement proper cache expiration policies  
- Monitor memory usage in production
- Use memory profilers during development

---

## 🔍 Debugging and Development Issues

### Issue 12: "Debugging domain events and handlers"

**Symptoms:**
- Events not firing as expected
- Difficulty tracing event flow
- Handler execution order issues

**Root Cause:**
Lack of logging and debugging infrastructure for domain events.

**Solution:**
```csharp
// Add comprehensive logging to event handlers
public class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
{
    private readonly ILogger<UserCreatedDomainEventHandler> _logger;

    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = nameof(UserCreatedDomainEvent),
            ["UserId"] = notification.UserId,
            ["CorrelationId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString()
        });

        _logger.LogInformation("Handling UserCreatedDomainEvent for user: {UserId}", notification.UserId);

        try
        {
            // Handler logic
            await SendWelcomeEmailAsync(notification);
            
            _logger.LogInformation("Successfully handled UserCreatedDomainEvent for user: {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserCreatedDomainEvent for user: {UserId}", notification.UserId);
            throw;
        }
    }
}

// Create debugging extensions
public static class DebuggingExtensions
{
    public static void LogDomainEvents(this ILogger logger, AggregateRoot aggregate)
    {
        var events = aggregate.GetDomainEvents();
        if (events.Any())
        {
            logger.LogDebug("Domain events raised by {AggregateType}: {EventTypes}", 
                aggregate.GetType().Name, 
                string.Join(", ", events.Select(e => e.GetType().Name)));
        }
    }
}

// Usage in command handlers
public async Task<Result<UserId>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    var user = new User(/* parameters */);
    
    _logger.LogDomainEvents(user); // Debug domain events
    
    _repository.Add(user);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return Result<UserId>.Success(user.Id);
}
```

**Prevention:**
- Implement structured logging throughout your application
- Use correlation IDs to trace requests across services
- Create debugging endpoints for development environments

---

## 📊 Monitoring and Observability

### Issue 13: "Lack of application insights in production"

**Symptoms:**
- Difficult to troubleshoot production issues
- No visibility into application performance
- Unable to track business metrics

**Solution:**
```csharp
// Add comprehensive telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry tracking
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;

    public void TrackBusinessMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackMetric(metricName, value, properties);
        _logger.LogInformation("Business metric tracked: {MetricName} = {Value}", metricName, value);
    }

    public void TrackCustomEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties);
        _logger.LogInformation("Custom event tracked: {EventName}", eventName);
    }
}

// Use in domain event handlers
public class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly ITelemetryService _telemetry;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Track business metrics
        _telemetry.TrackBusinessMetric("orders.created", 1, new Dictionary<string, string>
        {
            ["customer_id"] = notification.CustomerId.ToString(),
            ["order_value"] = notification.Total.Amount.ToString()
        });

        _telemetry.TrackCustomEvent("order.created", new Dictionary<string, string>
        {
            ["order_id"] = notification.OrderId.ToString(),
            ["total_amount"] = notification.Total.Amount.ToString()
        });

        // Continue with handler logic...
    }
}
```

**Health Checks:**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"))
    .AddRabbitMQ(builder.Configuration.GetConnectionString("RabbitMQ"));

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**Prevention:**
- Implement health checks for all external dependencies
- Set up alerting for critical business metrics
- Use distributed tracing for microservice communication
- Create dashboards for key performance indicators

---

## 🎯 Best Practices for Prevention

### Development Environment Setup
```bash
# Use Docker Compose for consistent development environment
# docker-compose.dev.yml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: dev_db
      POSTGRES_USER: dev_user
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"

volumes:
  postgres_data:
```

### Code Quality Tools
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS8625;CS8618</WarningsNotAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.12.0.78982">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### Testing Strategy
```csharp
// Base test class for integration tests
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected async Task<T> SeedEntityAsync<T>(T entity) where T : class
    {
        DbContext.Set<T>().Add(entity);
        await DbContext.SaveChangesAsync();
        return entity;
    }

    public void Dispose()
    {
        Scope.Dispose();
        Client.Dispose();
    }
}
```

## 🆘 Getting Additional Help

If you're still experiencing issues:

1. **Check the logs** - Enable detailed logging and look for error patterns
2. **Review the documentation** - Ensure you're following the patterns correctly
3. **Create minimal reproducible examples** - Isolate the problem
4. **Search existing issues** - Your problem might already be solved
5. **Ask the community** - Join our Discord/Slack for real-time help
6. **Open an issue** - Provide detailed reproduction steps

Remember: Most issues stem from configuration problems or not following the established patterns. When in doubt, refer back to the examples and ensure you're implementing the SharedKernel patterns correctly.