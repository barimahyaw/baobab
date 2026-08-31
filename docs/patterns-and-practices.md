# Patterns and Best Practices

## Mastering the Baobab SharedKernel Patterns

This guide covers the essential patterns, best practices, and architectural decisions that make the SharedKernel architecture so powerful. These patterns have been battle-tested in production environments and will help you build robust, scalable microservices.

## 🎯 Core Architectural Patterns

### 1. Result Pattern

**Problem**: Traditional exception-based error handling creates unpredictable control flow and performance overhead.

**Solution**: Explicit success/failure handling using the Result pattern.

```csharp
// ❌ Traditional exception-based approach
public async Task<User> CreateUserAsync(CreateUserRequest request)
{
    if (string.IsNullOrEmpty(request.Email))
        throw new ValidationException("Email is required");
        
    if (await _repository.ExistsAsync(request.Email))
        throw new BusinessException("User already exists");
        
    return new User(request.Email, request.FirstName);
}

// ✅ Result pattern approach
public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
{
    if (string.IsNullOrEmpty(request.Email))
        return Result<User>.Fail(Error.Validation("Email is required"));
        
    if (await _repository.ExistsAsync(request.Email))
        return Result<User>.Fail(Error.Conflict("User already exists"));
        
    var user = new User(request.Email, request.FirstName);
    return Result<User>.Success(user);
}
```

**Benefits**:
- Explicit error handling
- Better performance (no exception overhead)
- Composable error results
- Clear method signatures

**Usage Guidelines**:
- Use `Result<T>` for operations that return data
- Use `Result` for operations that only indicate success/failure
- Chain results using the built-in methods
- Handle all error cases explicitly

### 2. CQRS (Command Query Responsibility Segregation)

**Problem**: Mixed read/write operations create complex, hard-to-maintain code.

**Solution**: Separate commands (writes) from queries (reads).

```csharp
// Commands - Change system state
public record CreateUserCommand(string Email, string FirstName) : ICommand<Result<UserId>>;
public record UpdateUserCommand(UserId Id, string FirstName, string LastName) : ICommand<Result>;
public record DeleteUserCommand(UserId Id) : ICommand<Result>;

// Queries - Read data
public record GetUserQuery(UserId Id) : IQuery<Result<UserDto>>;
public record GetUsersQuery(int PageNumber, int PageSize) : IPaginatedQuery<UserDto>;
public record SearchUsersQuery(string SearchTerm) : IQuery<Result<IEnumerable<UserDto>>>;
```

**Command Handler Example**:
```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result<UserId>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public async Task<Result<UserId>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email: {Email}", request.Email);

        // Validate business rules
        if (await _userRepository.ExistsAsync(request.Email, cancellationToken))
        {
            return Result<UserId>.Fail(
                Error.Conflict("User.EmailExists", "A user with this email already exists"));
        }

        // Create domain entity
        var userId = new UserId(Guid.NewGuid());
        var user = new User(userId, request.Email, request.FirstName);

        // Persist changes
        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created successfully with ID: {UserId}", userId.Value);
        
        return Result<UserId>.Success(userId);
    }
}
```

**Query Handler Example**:
```csharp
public class GetUserQueryHandler : IQueryHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserReadRepository _userReadRepository;
    private readonly IMapper _mapper;
    private readonly ICacheManager _cache;

    public async Task<Result<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Try cache first
        var cacheKey = $"user:{request.Id.Value}";
        var cachedUser = await _cache.GetAsync<UserDto>(cacheKey, cancellationToken);
        if (cachedUser != null)
            return Result<UserDto>.Success(cachedUser);

        // Query database
        var user = await _userReadRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<UserDto>.Fail(Error.NotFound("User not found"));

        var userDto = _mapper.Map<UserDto>(user);
        
        // Cache the result
        await _cache.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(15), cancellationToken);
        
        return Result<UserDto>.Success(userDto);
    }
}
```

### 3. Domain Events Pattern

**Problem**: Business operations often trigger multiple side effects, creating tightly coupled code.

**Solution**: Use domain events to decouple business operations from their side effects.

```csharp
// Domain event definition
public record UserCreatedDomainEvent(
    UserId UserId,
    EmailAddress Email,
    FirstName FirstName,
    LastName LastName,
    DateTime CreatedAt) : DomainEvent;

// Raise event in aggregate root
public class User : AggregateRoot
{
    public User(UserId id, EmailAddress email, FirstName firstName, LastName lastName)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;

        // Raise domain event
        RaiseDomainEvent(new UserCreatedDomainEvent(Id, Email, FirstName, LastName, CreatedAt));
    }
}

// Handle domain event
public class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
{
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<UserCreatedDomainEventHandler> _logger;

    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UserCreatedDomainEvent for user: {UserId}", notification.UserId);

        try
        {
            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(
                notification.Email.Value, 
                notification.FirstName.Value,
                cancellationToken);

            // Could also:
            // - Update analytics
            // - Send to external systems
            // - Create user profile in other services
            
            _logger.LogInformation("Welcome email sent to user: {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email for user: {UserId}", notification.UserId);
            // Don't throw - this is a side effect, not core business logic
        }
    }
}
```

### 4. Outbox Pattern

**Problem**: Domain events need to be published reliably, even if external message bus is unavailable.

**Solution**: Store events in database transaction, then publish them asynchronously.

```csharp
// Outbox message entity
public class OutboxMessage : Entity
{
    public long Id { get; set; }
    public string Type { get; set; } = default!;
    public string Assembly { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedDateUtc { get; set; }
    public int ProcessingAttempts { get; set; }
    public string? Error { get; set; }
}

// Background job to process outbox messages
public class OutBoxMessagesProcessingJob : IOutBoxMessagesProcessingJob
{
    public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var unprocessedMessages = await _repository.GetUnprocessedMessagesAsync(cancellationToken);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                // Deserialize and publish event
                var domainEvent = JsonSerializer.Deserialize(message.Content, Type.GetType(message.Type)!);
                await _publisher.Publish(domainEvent, cancellationToken);

                // Mark as processed
                message.ProcessedDateUtc = DateTime.UtcNow;
                await _repository.UpdateAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                message.ProcessingAttempts++;
                message.Error = ex.Message;
                await _repository.UpdateAsync(message, cancellationToken);
            }
        }
    }
}
```

## 🏛️ Domain-Driven Design Best Practices

### Value Objects

**Guidelines**:
- Immutable by design
- Equality based on value, not identity
- Self-validating
- Express domain concepts clearly

```csharp
public class EmailAddress : ValueObject
{
    public string Value { get; private set; }

    private EmailAddress() { } // EF Core constructor

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<EmailAddress>.Fail(Error.Validation("Email cannot be empty"));

        if (email.Length > 255)
            return Result<EmailAddress>.Fail(Error.Validation("Email cannot exceed 255 characters"));

        if (!IsValidEmailFormat(email))
            return Result<EmailAddress>.Fail(Error.Validation("Invalid email format"));

        return Result<EmailAddress>.Success(new EmailAddress(email.ToLowerInvariant()));
    }

    private static bool IsValidEmailFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(EmailAddress email) => email.Value;
}
```

### Aggregate Roots

**Guidelines**:
- Maintain invariants across the aggregate
- Only reference other aggregates by ID
- Raise domain events for important business occurrences
- Keep aggregates small and focused

```csharp
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();
    
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public Money Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Result AddItem(ProductId productId, Money unitPrice, int quantity)
    {
        // Business rule: Cannot modify completed orders
        if (Status == OrderStatus.Completed)
            return Result.Fail(Error.BusinessRule("Cannot modify completed order"));

        // Business rule: Maximum 10 items per order
        if (_items.Count >= 10)
            return Result.Fail(Error.BusinessRule("Maximum 10 items per order"));

        var orderItem = new OrderItem(productId, unitPrice, quantity);
        _items.Add(orderItem);
        
        RecalculateTotal();
        
        RaiseDomainEvent(new OrderItemAddedDomainEvent(Id, productId, quantity));
        
        return Result.Success();
    }

    private void RecalculateTotal()
    {
        Total = _items.Sum(item => item.TotalPrice);
    }
}
```

### Domain Services

**When to use**:
- Complex business logic that doesn't belong to a single entity
- Operations involving multiple aggregates
- External system interactions within domain logic

```csharp
public interface IPricingDomainService
{
    Task<Money> CalculateDiscountedPriceAsync(
        ProductId productId, 
        CustomerId customerId, 
        int quantity,
        CancellationToken cancellationToken);
}

public class PricingDomainService : IPricingDomainService
{
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;

    public async Task<Money> CalculateDiscountedPriceAsync(
        ProductId productId, 
        CustomerId customerId, 
        int quantity,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

        var basePrice = product.Price * quantity;
        
        // Apply volume discount
        var volumeDiscount = quantity switch
        {
            >= 100 => 0.15m,
            >= 50 => 0.10m,
            >= 10 => 0.05m,
            _ => 0m
        };

        // Apply customer tier discount
        var customerDiscount = customer.Tier switch
        {
            CustomerTier.Platinum => 0.20m,
            CustomerTier.Gold => 0.15m,
            CustomerTier.Silver => 0.10m,
            _ => 0m
        };

        var totalDiscount = Math.Max(volumeDiscount, customerDiscount);
        return basePrice * (1 - totalDiscount);
    }
}
```

## 🔧 Infrastructure Best Practices

### Caching Strategy

```csharp
public class CachingService : ICachingService
{
    private readonly IDistributedCacheManager _distributedCache;
    private readonly IMemoryCacheManager _memoryCache;
    private readonly ILogger<CachingService> _logger;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Try memory cache first (fastest)
        var memoryResult = await _memoryCache.GetAsync<T>(key, cancellationToken);
        if (memoryResult != null)
        {
            _logger.LogDebug("Cache hit in memory cache for key: {Key}", key);
            return memoryResult;
        }

        // Try distributed cache (Redis)
        var distributedResult = await _distributedCache.GetAsync<T>(key, cancellationToken);
        if (distributedResult != null)
        {
            _logger.LogDebug("Cache hit in distributed cache for key: {Key}", key);
            
            // Populate memory cache for next time
            await _memoryCache.SetAsync(key, distributedResult, TimeSpan.FromMinutes(5), cancellationToken);
            return distributedResult;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        // Set in both caches
        await _distributedCache.SetAsync(key, value, expiration, cancellationToken);
        await _memoryCache.SetAsync(key, value, TimeSpan.FromMinutes(Math.Min(5, expiration.TotalMinutes)), cancellationToken);
        
        _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, expiration);
    }
}
```

### Resilience Patterns

```csharp
public class ResilientHttpClient : IResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ILogger<ResilientHttpClient> _logger;

    public ResilientHttpClient(HttpClient httpClient, ILogger<ResilientHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _retryPolicy = CreateRetryPolicy();
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogDebug("Making HTTP GET request to: {Endpoint}", endpoint);
                return await _httpClient.GetAsync(endpoint, cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<T>(content);
            }

            _logger.LogWarning("HTTP request failed with status: {StatusCode}", response.StatusCode);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request failed for endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode >= HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
    }
}
```

## 📊 Performance Best Practices

### Pagination

```csharp
public record GetUsersQuery(int PageNumber = 1, int PageSize = 20, string? SearchTerm = null) 
    : IPaginatedQuery<UserDto>
{
    // Ensure valid pagination parameters
    public int PageNumber { get; init; } = Math.Max(1, PageNumber);
    public int PageSize { get; init; } = Math.Min(100, Math.Max(1, PageSize)); // Max 100 items per page
}

public class GetUsersQueryHandler : IPaginatedQueryHandler<GetUsersQuery, UserDto>
{
    public async Task<PaginatedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(u => 
                u.FirstName.Value.Contains(request.SearchTerm) ||
                u.LastName.Value.Contains(request.SearchTerm) ||
                u.Email.Value.Contains(request.SearchTerm));
        }

        // Get total count (for pagination info)
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id.Value,
                u.FirstName.Value,
                u.LastName.Value,
                u.Email.Value,
                u.Phone != null ? u.Phone.Value : null,
                u.CreatedAt,
                u.UpdatedAt,
                u.IsActive))
            .ToListAsync(cancellationToken);

        return PaginatedResult<UserDto>.Success(
            users,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
```

### Database Optimization

```csharp
// Entity Configuration for optimal querying
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        // Indexes for common queries
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.CreatedAt);
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => new { u.IsActive, u.CreatedAt }); // Composite index

        // Value object conversions
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value).Value!)
            .HasMaxLength(255);

        // Ignore domain events (they're not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
```

## 🧪 Testing Best Practices

### Unit Testing Domain Logic

```csharp
[Fact]
public void User_ChangeEmail_ShouldRaiseDomainEvent()
{
    // Arrange
    var user = UserTestData.CreateValidUser();
    var newEmail = EmailAddress.Create("new@example.com").Value!;
    
    // Act
    var result = user.ChangeEmail(newEmail);
    
    // Assert
    result.Should().Succeed();
    user.Email.Should().Be(newEmail);
    
    var domainEvent = user.GetDomainEvents().Should().ContainSingle()
        .Which.Should().BeOfType<UserEmailChangedDomainEvent>().Subject;
    
    domainEvent.UserId.Should().Be(user.Id);
    domainEvent.NewEmail.Should().Be(newEmail);
}

[Fact]
public void User_ChangeEmail_WithSameEmail_ShouldNotRaiseEvent()
{
    // Arrange
    var user = UserTestData.CreateValidUser();
    var currentEmail = user.Email;
    
    // Act
    var result = user.ChangeEmail(currentEmail);
    
    // Assert
    result.Should().Succeed();
    user.GetDomainEvents().Should().BeEmpty();
}
```

### Integration Testing

```csharp
[Collection("Database")]
public class CreateUserCommandHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;

    public CreateUserCommandHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUser()
    {
        // Arrange
        var handler = _scope.ServiceProvider.GetRequiredService<CreateUserCommandHandler>();
        var command = new CreateUserCommand("test@example.com", "John", "Doe");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Succeed();
        
        var user = await _context.Users.FindAsync(result.Value);
        user.Should().NotBeNull();
        user!.Email.Value.Should().Be("test@example.com");
        user.FirstName.Value.Should().Be("John");
        user.LastName.Value.Should().Be("Doe");
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
```

## 🚀 Next Steps

Ready to apply these patterns? Check out:

1. **[Practical Examples](./examples.md)** - See these patterns in action
2. **[Team Architecture Handoff Guide](./team-architecture-handoff-guide.md)** - Domain modeling and CQRS in depth
3. **[Troubleshooting Guide](./troubleshooting.md)** - Common issues and solutions