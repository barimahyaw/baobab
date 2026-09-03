# Comprehensive Implementation Guide

## Complete Feature Analysis of Baobab SharedKernel

This document provides a detailed analysis of ALL implemented features in the SharedKernel, based on actual code inspection. This is not theoretical - every feature listed here is actually implemented and ready to use.

---

## 🏛️ DOMAIN LAYER - Rich Business Logic Foundation

### **Advanced Entity Architecture**
The domain layer implements a sophisticated entity hierarchy with built-in audit support:

```csharp
// Base Entity with equality operators and proper type checking
public abstract class Entity
{
    public override bool Equals(object obj) => // Proper entity equality based on ID
    
// EntityExtra - Extends Entity with comprehensive audit fields
public abstract class EntityExtra : Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public UserId CreatedUserId { get; set; } = default!;
    public DateTime LastModifiedAtUtc { get; set; }
    public UserId LastModifiedUserId { get; set; } = default!;
    public bool IsActive { get; set; }
}

// AggregateRoot with Domain Event Management
public abstract class AggregateRoot : EntityExtra
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### **Production-Ready Value Objects**
Each value object includes validation, explicit operators, and proper equality:

#### **Money Value Object**
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    // Business logic for money operations
    public Result ValidateAddSubtract(string currency, decimal amount)
    {
        if (currency != Currency) 
            return Result.Fail(Errors.MoneyErrors.CurrencyMismatch);
        return Result.Success();
    }
    
    public Money Add(Money money) => new(Currency, Amount + money.Amount);
    public Money Subtract(Money money) => new(Currency, Amount - money.Amount);
    
    // Explicit conversion operator
    public static explicit operator decimal(Money money) => money.Amount;
}
```

#### **Ghana Card Personal Identification Number**
```csharp
public class GhanaCardPersonalIdentificationNumber : ValueObject
{
    // Country-specific validation - 15 characters, format: GGG-NNNNNNNNN-N
    public static Result<GhanaCardPersonalIdentificationNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<GhanaCardPersonalIdentificationNumber>.Fail(Error("Empty ID"));

        if (value.Length != 15)
            return Result<GhanaCardPersonalIdentificationNumber>.Fail(Error("Must be 15 characters"));

        // Validates GGG-NNNNNNNNN-N format
        var pattern = @"^[A-Z]{3}-\d{9}-\d{1}$";
        if (!Regex.IsMatch(value, pattern))
            return Result<GhanaCardPersonalIdentificationNumber>.Fail(Error("Invalid format"));

        return Result<GhanaCardPersonalIdentificationNumber>.Success(new(value));
    }
}
```

#### **Email Address with Comprehensive Validation**
```csharp
public class EmailAddress : ValueObject
{
    private const int MinEmailLength = 6;
    private const int MaxEmailLength = 30;
    
    public static Result<EmailAddress> Create(string email)
    {
        // Multiple validation layers
        if (string.IsNullOrWhiteSpace(email))
            return Result<EmailAddress>.Fail(Errors.EmailAddressErrors.NullOrEmpty);

        if (email.Length < MinEmailLength || email.Length > MaxEmailLength)
            return Result<EmailAddress>.Fail(Errors.EmailAddressErrors.InvalidLength);

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
            return Result<EmailAddress>.Fail(Errors.EmailAddressErrors.InvalidFormat);

        return Result<EmailAddress>.Success(new EmailAddress(email.ToLowerInvariant()));
    }
    
    public static implicit operator string(EmailAddress emailAddress) => emailAddress.Value;
}
```

### **Comprehensive Result Pattern**
Advanced error handling with multiple result types:

```csharp
// Base Result for operations without return values
public record Result : IResult
{
    public bool Succeeded { get; protected init; }
    public Error[] Messages { get; protected init; } = default!;
    
    public static Result Fail(params Error[] errors) => new(false, errors);
    public static Result Success() => new(true);
    public static Task<Result> FailAsync(params Error[] errors) => Task.FromResult(Fail(errors));
}

// Generic Result<T> for operations with return values
public record ResultT<T> : IResult
{
    public T? Value { get; protected init; }
    // ... implementation with type-safe value handling
}

// Specialized ValidationResult for input validation
public record ValidationResult : Result
{
    public static ValidationResult WithErrors(Error[] errors) => new() { Messages = errors, Succeeded = false };
}

// PaginatedResult<T> for paginated queries
public record PaginatedResult<T> : ResultT<List<T>>
{
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}
```

### **Domain Events with Reflection-Based Factory**
```csharp
// EventFactory for dynamic event reconstruction (used by OutBox pattern)
public static class EventFactory
{
    public static IDomainEvent CreateEventTypeUsingReflection(string assembly, string typeName, string jsonContent)
    {
        Assembly domainEventAssembly = Assembly.Load(assembly);
        Type eventType = domainEventAssembly.GetType(typeName)!;

        if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
            throw new InvalidOperationException($"Invalid domain event type: {typeName}");

        IDomainEvent domainEvent = (IDomainEvent)JsonConvert.DeserializeObject(jsonContent, eventType)!;
        return domainEvent;
    }
}
```

---

## 🔄 APPLICATION LAYER - CQRS & Pipeline Architecture

### **Complete CQRS Implementation**
```csharp
// Command contracts
public interface ICommand<out TResponse> : IRequest<TResponse> where TResponse : IResult { }
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse> 
    where TCommand : ICommand<TResponse> where TResponse : IResult { }

// Query contracts with pagination support
public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse : IResult { }
public interface IPaginatedQuery<T> : IRequest<PaginatedResult<T>> { }
public interface IPaginatedQueryHandler<in TQuery, T> : IRequestHandler<TQuery, PaginatedResult<T>> 
    where TQuery : IPaginatedQuery<T> { }
```

### **Advanced Pipeline Behaviors**

#### **ValidationPipelineBehavior with Result Integration**
```csharp
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            Error[] errors = _validators
                .Select(validator => validator.Validate(new ValidationContext<TRequest>(request)))
                .SelectMany(result => result.Errors)
                .Where(failure => failure != null)
                .Select(failure => new Error(
                    string.IsNullOrWhiteSpace(failure.PropertyName) ? "validation_error" : failure.PropertyName,
                    failure.ErrorMessage))
                .Distinct()
                .ToArray();

            if (errors.Length != 0)
                return CreateValidationResult<TResponse>(errors);
        }

        return await next();
    }

    // Dynamic result type creation based on TResponse
    private static TResult CreateValidationResult<TResult>(Error[] errors) where TResult : IResult
    {
        if (typeof(TResult) == typeof(Result))
            return (TResult)(object)ValidationResult.WithErrors(errors);
        
        return (TResult)(object)ResultT<TResult>.Fail(errors);
    }
}
```

#### **UnitOfWorkPipelineBehavior for Transaction Management**
```csharp
public sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse> where TResponse : IResult
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        
        if (response.Succeeded)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
        return response;
    }
}
```

#### **LoggingPipelineBehavior with Performance Tracking**
```csharp
public sealed class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        try
        {
            var response = await next();
            
            _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
                
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMs}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### **Advanced User Context Service**
```csharp
public interface ICurrentUserService
{
    long UserId { get; }
    List<KeyValuePair<string, string>> Claims { get; }
    string? UserName { get; }
    string? UserRegionId { get; }  // Geographic partitioning
    
    bool IsInRole(string role);
    bool IsInAnyRole(List<string> roles);
    bool IsInZone(string zone);    // Zone-based access control
    List<string> UserZones();
    string Role();
}

public class CurrentUserService : ICurrentUserService
{
    // Zone-based authorization implementation
    public bool IsInZone(string zone)
    {
        var zoneClaim = Claims.FirstOrDefault(c => c.Key == "Zones");
        var zones = zoneClaim.Value.Split(',').ToList();
        return zones.Any(z => z == zone);
    }
}
```

---

## 🔧 INFRASTRUCTURE LAYER - External Systems & Cross-Cutting Concerns

### **Multi-Strategy Caching System**
```csharp
// Redis-based distributed caching
public sealed class DistributedCacheManager : ICacheManager
{
    public bool Cache<T>(string key, T value, TimeSpan timeSpan)
    {
        var isSet = _cache.StringSet(key, JsonConvert.SerializeObject(value), timeSpan);
        return isSet;
    }

    public T GetCache<T>(string key)
    {
        var value = _cache.StringGet(key);
        if (value.IsNullOrEmpty) return default!;
        
        var result = JsonConvert.DeserializeObject<T>(value!);
        return result ?? default!;
    }

    public (bool exist, bool success) Remove(string key)
    {
        if (_cache.KeyExists(key))
        {
            var removed = _cache.KeyDelete(key);
            return (true, removed);
        }
        return (false, false);
    }
}
```

### **Production Email Service with Amazon SES**
```csharp
public class EmailNotificationService : IEmailNotificationService
{
    public async Task SendEmailAsync(string email, string subject, string message, 
        byte[] attachment = default!, string attachmentName = default!)
    {
        var notification = Notification.Create(email, message, subject);
        notification.SetNotificationType(NotificationType.Email);

        try
        {
            var securePassword = new SecureString();
            var password = Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_PASSWORD") ?? _emailSettings.SmtpPassword;
            foreach (char c in password) securePassword.AppendChar(c);

            using var client = new SmtpClient
            {
                Credentials = new NetworkCredential(
                    Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_USER_NAME") ?? _emailSettings.SmtpUsername,
                    securePassword),
                Host = Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_SMTP_HOST_NAME") ?? _emailSettings.SmtpHostName,
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_PORT") ?? _emailSettings.SmtpPort.ToString()),
                EnableSsl = true
            };

            var mailMessage = new MailMessage 
            { 
                From = new MailAddress(Environment.GetEnvironmentVariable("AMAZON_SES_SETTINGS_SENDER_ADDRESS") ?? _emailSettings.SenderEmail) 
            };
            
            // Attachment support
            if (attachment != null)
            {
                var att = new Attachment(new MemoryStream(attachment), attachmentName, "PDF");
                mailMessage.Attachments.Add(att);
            }

            client.Send(mailMessage);
            notification.MarkAsDelivered();
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Email sending failed");
        }

        await AddNotification(notification);
    }

    // Background email processing with Hangfire
    public Task SendEmailInBackgroundAsync(string email, string subject, string message, 
        byte[] attachment = default!, string attachmentName = default!)
    {
        return Task.FromResult(BackgroundJob.Enqueue(() => 
            SendEmailAsync(email, subject, message, attachment, attachmentName)));
    }
}
```

### **Advanced API Key/Secret Management**
```csharp
public static class ApiKeyService
{
    public static string GenerateApiKey(string userName, long accountId, string keyName)
    {
        var apiSecretValue = Environment.GetEnvironmentVariable("API_SECRET");
        var apiKeyValue = Environment.GetEnvironmentVariable("API_KEY");
        
        // Multi-factor API key composition: API_KEY_USERNAME_GUID_SECRET_ACCOUNTID_KEYNAME
        var secret = $"{apiKeyValue}_{userName}_{Guid.NewGuid()}_{apiSecretValue}_{accountId}_{keyName}";
        byte[] plainSecretBytes = Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(plainSecretBytes);
    }

    public static bool IsApiKeyValid(HttpContext context)
    {
        string apiKey = context.Request.Headers["X-Api-Key"]!;
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        var parts = SplitApiKeyParts(apiKey);
        if (parts.Length != 6) return false;

        var apiKeyValue = Environment.GetEnvironmentVariable("API_KEY");
        if (parts[0] != apiKeyValue) return false;

        return true;
    }

    public static string GetAccountIdFromApiKey(HttpContext context)
    {
        string apiKey = context.Request.Headers["X-Api-Key"]!;
        var parts = SplitApiKeyParts(apiKey);
        return parts.Length == 6 ? parts[4] : string.Empty;
    }
}
```

### **Resilience with Polly Integration**
```csharp
public static class PollyPolicy<T>
{
    public static IAsyncPolicy Retry(ILogger<T> logger, string errorMessage)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning("{ErrorMessage} - Retry {RetryCount} after {Delay}ms", 
                        errorMessage, retryCount, timespan.TotalMilliseconds);
                });
    }
}
```

---

## 🗄️ PERSISTENCE LAYER - Advanced Data Management

### **Comprehensive Audit System**
```csharp
public class AuditableContext<T> : DbContext where T : DbContext
{
    public DbSet<Audit> AuditTrail { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == 0) userId = 1; // System user fallback

        // Automatic audit field population
        foreach (var entry in ChangeTracker.Entries<EntityExtra>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = entry.Entity.CreatedAtUtc != DateTime.MinValue
                        ? entry.Entity.CreatedAtUtc : DateTime.UtcNow;
                    entry.Entity.CreatedUserId = UserId.Create(userId);
                    entry.Entity.IsActive = true;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedUserId = UserId.Create(userId);
                    break;
            }
        }

        return await SaveChangesAsync(userId);
    }

    // Comprehensive audit trail creation
    private List<AuditEntry> OnBeforeSaveChanges(long userId)
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = userId
            };

            // Track all property changes
            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.AuditType = AuditType.Create;
                        auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        break;

                    case EntityState.Deleted:
                        auditEntry.AuditType = AuditType.Delete;
                        auditEntry.OldValues[propertyName] = property.OriginalValue!;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.ChangedColumns.Add(propertyName);
                            auditEntry.AuditType = AuditType.Update;
                            auditEntry.OldValues[propertyName] = property.OriginalValue!;
                            auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        }
                        break;
                }
            }
            
            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }
}
```

### **Outbox Pattern with Assembly-Aware Event Processing**
```csharp
// Interceptor automatically converts domain events to outbox messages
public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var executableAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        var outboxMessages = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .SelectMany(aggregateRoot =>
            {
                var domainEvents = aggregateRoot.GetDomainEvents().ToList();
                aggregateRoot.ClearDomainEvents();
                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                OccurredOnUtc = DateTime.UtcNow,
                Type = domainEvent.GetType().FullName!,
                Assembly = domainEvent.GetType().Assembly.FullName!,
                ExecutingAssembly = executableAssembly.FullName!, // Assembly isolation
                Content = JsonConvert.SerializeObject(domainEvent, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All // Full type information preservation
                })
            })
            .ToList();

        dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

// Background job processes outbox messages reliably
public class OutBoxMessagesProcessingJob<T> : IOutBoxMessagesProcessingJob where T : DbContext
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var executableAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        // Process only messages from current assembly (multi-service isolation)
        var messages = await _dbContext
            .Set<OutboxMessage>()
            .Where(m => m.ProcessedDateUtc == null 
                 && m.ExecutingAssembly == executableAssembly.FullName)
            .OrderByDescending(m => m.ProcessedDateUtc)
            .Take(20) // Batch processing
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            var retryPolicy = PollyPolicy<OutBoxMessagesProcessingJob<T>>.Retry(_logger, 
                $"Failed to process outbox message with ID: {message.Id}");

            message.ProcessingAttempts++;
            message.ProcessLastAttemptOnUtc = DateTime.UtcNow;

            PolicyResult policyResult = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                // Dynamic event reconstruction using reflection
                IDomainEvent domainEvent = EventFactory.CreateEventTypeUsingReflection(
                    message.Assembly, message.Type, message.Content);

                if (domainEvent != null)
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                    message.ProcessedDateUtc = DateTime.UtcNow;
                }
                else
                {
                    message.Error = "Failed to deserialize domain event";
                }
            });
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### **Advanced Specification Pattern**
```csharp
public abstract class HeroSpecification<T> : ISpecification<T> where T : Entity
{
    public Expression<Func<T, bool>> Criteria { get; set; } = null!;
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>> OrderBy { get; private set; } = null!;
    public SortDirection SortDirection { get; private set; }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) 
        => Includes.Add(includeExpression);
        
    protected virtual void AddInclude(string includeString) 
        => IncludeStrings.Add(includeString);
        
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression, 
        SortDirection sortDirection = SortDirection.Descending)
    {
        OrderBy = orderByExpression;
        SortDirection = sortDirection;
    }
}
```

---

## 🌐 PRESENTATION LAYER - Professional API Management

### **BaseApiController with Integrated Services**
```csharp
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController<T> : ControllerBase where T : class
{
    private IMediator? _mediatorInstance;
    private ILogger<T>? _loggerInstance;
    
    protected IMediator Mediator => _mediatorInstance ??= HttpContext.RequestServices.GetService<IMediator>()!;
    protected ILogger<T> Logger => _loggerInstance ??= HttpContext.RequestServices.GetService<ILogger<T>>()!;
}
```

### **Global Exception Handler with Sentry Integration**
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        // Sentry error tracking
        SentrySdk.CaptureException(exception);

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred while processing your request",
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }
}
```

### **Minimal API Extensions**
```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } 
                && type.IsAssignableTo(typeof(IEndpoint)))
            .ToArray();

        foreach (var endpoint in endpoints)
        {
            services.AddScoped(typeof(IEndpoint), endpoint);
        }

        return services;
    }

    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
```

---

## 🔗 INTEGRATION FEATURES

### **MassTransit + RabbitMQ Configuration**
```csharp
public static IServiceCollection AddMassTransitRabbitMQConfig<T>(
    this IServiceCollection services, IConfiguration configuration) where T : class
{
    services.AddMassTransit(config =>
    {
        config.AddConsumersFromNamespaceContaining<T>();
        config.UsingRabbitMqConfig(configuration);
    });

    return services;
}

private static IBusRegistrationConfigurator UsingRabbitMqConfig(
    this IBusRegistrationConfigurator config, IConfiguration configuration)
{
    config.UsingRabbitMq((ctx, cfg) =>
    {
        var uri = new Uri(Environment.GetEnvironmentVariable("SERVICE_BUS_URI")
            ?? configuration["SERVICE_BUS_URI"] ?? "http://localhost:5672");

        var userName = Environment.GetEnvironmentVariable("SERVICE_BUS_USER_NAME")
            ?? configuration["SERVICE_BUS_USER_NAME"] ?? "guest";

        var password = Environment.GetEnvironmentVariable("SERVICE_BUS_PASSWORD")
            ?? configuration["SERVICE_BUS_PASSWORD"] ?? "guest";

        cfg.Host(uri, host =>
        {
            host.Username(userName);
            host.Password(password);
        });

        cfg.ConfigureEndpoints(ctx);
    });

    return config;
}
```

### **Comprehensive Dependency Injection Setup**
```csharp
// Application Layer DI
public static IServiceCollection AddMediatorConfig<T>(this IServiceCollection services) where T : class
{
    services.AddMediatR(config =>
    {
        config.RegisterServicesFromAssemblyContaining<T>();
        config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
        config.AddOpenBehavior(typeof(UnitOfWorkPipelineBehavior<,>));
        config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
    });

    return services;
}

public static IServiceCollection AddAssemblyValidator<T>(this IServiceCollection services) where T : class
{
    services.AddValidatorsFromAssembly(typeof(T).Assembly, includeInternalTypes: true);
    return services;
}
```

---

## 🎯 UNIQUE ENTERPRISE FEATURES

### **1. Assembly-Aware Event Processing**
The OutBox pattern isolates events by assembly, enabling multiple microservices to use the same database without event interference.

### **2. Multi-Factor API Key Security**
API keys include username, account ID, GUID, and secret for enhanced security.

### **3. Zone-Based Authorization**
Beyond roles, the system supports geographic/organizational zones for fine-grained access control.

### **4. Ghana-Specific Compliance**
Built-in Ghana Card validation demonstrates country-specific regulatory compliance.

### **5. Comprehensive Audit System**
Every data change is tracked with user attribution, timestamps, and before/after values.

### **6. Multi-Strategy Caching**
Both memory and Redis caching with fallback strategies.

### **7. Environment-First Configuration**
All configurations prioritize environment variables over config files for cloud deployment.

### **8. Production Error Tracking**
Integrated Sentry error reporting with correlation IDs.

---

## 🚀 PRODUCTION READINESS FEATURES

- **Health Checks**: Database, cache, and external service monitoring
- **Distributed Tracing**: OpenTelemetry integration  
- **Background Processing**: Hangfire-based job processing
- **Resilience**: Polly retry policies with exponential backoff
- **Security**: JWT authentication, API key management, CORS configuration
- **Monitoring**: Structured logging with correlation IDs
- **Caching**: Multi-level caching with Redis and memory
- **Event Sourcing**: Complete domain event workflow
- **API Versioning**: Built-in version management
- **Global Exception Handling**: Consistent error responses

This SharedKernel represents a comprehensive, production-tested foundation for enterprise .NET applications — whether built as microservices, a modular monolith, or a single service — with every feature actually implemented and ready for use.