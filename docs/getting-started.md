# Getting Started with Baobab SharedKernel

This guide will walk you through creating your first microservice using the Baobab SharedKernel architecture. By the end, you'll have a fully functional microservice with CQRS, domain events, and all the architectural patterns implemented.

## 📋 Prerequisites

Before we begin, ensure you have:

- **.NET 10 SDK** installed
- **Docker Desktop** (for infrastructure dependencies)
- **Your favorite IDE** (Visual Studio, VS Code, or Rider)
- **Basic understanding** of Clean Architecture and DDD concepts

## 🚀 Step 1: Set Up Your Development Environment

### Install Infrastructure Dependencies

We'll use Docker Compose to set up the required infrastructure services:

```bash
# Create a docker-compose.yml file
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: microservice_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
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
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"
      - "15672:15672"

volumes:
  postgres_data:
```

Start the infrastructure:

```bash
docker-compose up -d
```

## 🏗️ Step 2: Create Your First Microservice

Let's build a **User Management Service** as our example.

### Project Structure

```
UserService/
├── UserService.Domain/
├── UserService.Application/
├── UserService.Infrastructure/
├── UserService.Persistence/
├── UserService.Api/
└── UserService.sln
```

### Create the Solution

```bash
# Create solution and projects
dotnet new sln -n UserService

# Create projects
dotnet new classlib -n UserService.Domain
dotnet new classlib -n UserService.Application  
dotnet new classlib -n UserService.Infrastructure
dotnet new classlib -n UserService.Persistence
dotnet new webapi -n UserService.Api

# Add projects to solution
dotnet sln add UserService.Domain/UserService.Domain.csproj
dotnet sln add UserService.Application/UserService.Application.csproj
dotnet sln add UserService.Infrastructure/UserService.Infrastructure.csproj
dotnet sln add UserService.Persistence/UserService.Persistence.csproj
dotnet sln add UserService.Api/UserService.Api.csproj
```

## 📦 Step 3: Add SharedKernel References

Add the SharedKernel packages to your projects:

```xml
<!-- UserService.Domain/UserService.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\SharedKernel\Baobab.SharedKernel.Domain\Baobab.SharedKernel.Domain.csproj" />
  </ItemGroup>
</Project>
```

```xml
<!-- UserService.Application/UserService.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\UserService.Domain\UserService.Domain.csproj" />
    <ProjectReference Include="..\..\SharedKernel\Baobab.SharedKernel.Application\Baobab.SharedKernel.Application.csproj" />
  </ItemGroup>
</Project>
```

Continue this pattern for Infrastructure, Persistence, and Api projects.

## 🏛️ Step 4: Implement the Domain Layer

### Create Your First Aggregate Root

```csharp
// UserService.Domain/Entities/User.cs
using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.ValueObjects;
using UserService.Domain.Events;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.Entities;

public class User : AggregateRoot
{
    public UserId Id { get; private set; }
    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public EmailAddress Email { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private User() { } // EF Core constructor

    public User(
        UserId id,
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        PhoneNumber? phone = null)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;

        RaiseDomainEvent(new UserCreatedDomainEvent(Id, Email, FirstName, LastName));
    }

    public void UpdateProfile(FirstName firstName, LastName lastName, PhoneNumber? phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, FirstName, LastName, Phone));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, Email));
    }
}
```

### Create Domain Events

```csharp
// UserService.Domain/Events/UserCreatedDomainEvent.cs
using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Domain.Events;

public record UserCreatedDomainEvent(
    UserId UserId,
    EmailAddress Email,
    FirstName FirstName,
    LastName LastName) : DomainEvent;
```

```csharp
// UserService.Domain/Events/UserProfileUpdatedDomainEvent.cs
using Baobab.SharedKernel.Domain.Primitives;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Domain.Events;

public record UserProfileUpdatedDomainEvent(
    UserId UserId,
    FirstName FirstName,
    LastName LastName,
    PhoneNumber? Phone) : DomainEvent;
```

### Create Repository Interface

```csharp
// UserService.Domain/Repositories/IUserRepository.cs
using UserService.Domain.Entities;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken = default);
    void Add(User user);
    void Update(User user);
    void Remove(User user);
}
```

## 🔄 Step 5: Implement the Application Layer

### Create Commands

```csharp
// UserService.Application/Users/Commands/CreateUser/CreateUserCommand.cs
using Baobab.SharedKernel.Application.Abstractions.Messaging;
using Baobab.SharedKernel.Domain.Results;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone = null) : ICommand<Result<UserId>>;
```

```csharp
// UserService.Application/Users/Commands/CreateUser/CreateUserCommandValidator.cs
using FluentValidation;

namespace UserService.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number must be in valid international format");
    }
}
```

### Create Command Handler

```csharp
// UserService.Application/Users/Commands/CreateUser/CreateUserCommandHandler.cs
using Baobab.SharedKernel.Application.Abstractions.Data;
using Baobab.SharedKernel.Application.Abstractions.Messaging;
using Baobab.SharedKernel.Domain.Results;
using Baobab.SharedKernel.Domain.ValueObjects;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result<UserId>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserId>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Create value objects
        var firstNameResult = FirstName.Create(request.FirstName);
        if (!firstNameResult.Succeeded)
            return Result<UserId>.Fail(firstNameResult.Messages);

        var lastNameResult = LastName.Create(request.LastName);
        if (!lastNameResult.Succeeded)
            return Result<UserId>.Fail(lastNameResult.Messages);

        var emailResult = EmailAddress.Create(request.Email);
        if (!emailResult.Succeeded)
            return Result<UserId>.Fail(emailResult.Messages);

        PhoneNumber? phone = null;
        if (!string.IsNullOrEmpty(request.Phone))
        {
            var phoneResult = PhoneNumber.Create(request.Phone);
            if (!phoneResult.Succeeded)
                return Result<UserId>.Fail(phoneResult.Messages);
            phone = phoneResult.Value;
        }

        // Check if user already exists
        if (await _userRepository.ExistsAsync(emailResult.Value!, cancellationToken))
        {
            return Result<UserId>.Fail(
                Error.Validation("User.EmailExists", "A user with this email already exists"));
        }

        // Create user
        var userId = new UserId(Guid.NewGuid());
        var user = new User(
            userId,
            firstNameResult.Value!,
            lastNameResult.Value!,
            emailResult.Value!,
            phone);

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserId>.Success(userId);
    }
}
```

### Create Queries

```csharp
// UserService.Application/Users/Queries/GetUser/GetUserQuery.cs
using Baobab.SharedKernel.Application.Abstractions.Messaging;
using Baobab.SharedKernel.Domain.Results;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Application.Users.Queries.GetUser;

public record GetUserQuery(UserId UserId) : IQuery<Result<UserDto>>;
```

```csharp
// UserService.Application/Users/Queries/GetUser/UserDto.cs
namespace UserService.Application.Users.Queries.GetUser;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive);
```

## 🗄️ Step 6: Implement the Persistence Layer

### Create DbContext

```csharp
// UserService.Persistence/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Baobab.SharedKernel.Persistence.Audits.Contexts;
using UserService.Domain.Entities;

namespace UserService.Persistence;

public class ApplicationDbContext : AuditableContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

### Create Entity Configuration

```csharp
// UserService.Persistence/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasConversion(
                firstName => firstName.Value,
                value => FirstName.Create(value).Value!)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasConversion(
                lastName => lastName.Value,
                value => LastName.Create(value).Value!)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value).Value!)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Phone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? PhoneNumber.Create(value).Value : null)
            .HasMaxLength(20);

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.IsActive).IsRequired();
    }
}
```

### Implement Repository

```csharp
// UserService.Persistence/Repositories/UserRepository.cs
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace UserService.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Remove(User user)
    {
        _context.Users.Remove(user);
    }
}
```

## 🌐 Step 7: Create the API Layer

### Configure Services in Program.cs

```csharp
// UserService.Api/Program.cs
using UserService.Application;
using UserService.Infrastructure;
using UserService.Persistence;
using Baobab.SharedKernel.Application;
using Baobab.SharedKernel.Infrastructure;
using Baobab.SharedKernel.Persistence;
using Baobab.SharedKernel.Presentation;

var builder = WebApplication.CreateBuilder(args);

// Add SharedKernel services
builder.Services.AddMediatorConfig<Program>();
builder.Services.AddAssemblyValidator<Program>();
builder.Services.AddMassTransitRabbitMQConfig<Program>(builder.Configuration);

// Add layer services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddApiVersioning();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Create API Controller

```csharp
// UserService.Api/Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Baobab.SharedKernel.Presentation;
using Baobab.SharedKernel.Domain.ValueObjects;
using UserService.Application.Users.Commands.CreateUser;
using UserService.Application.Users.Queries.GetUser;

namespace UserService.Api.Controllers;

[ApiVersion("1.0")]
public class UsersController : BaseApiController<UsersController>
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        
        if (!result.Succeeded)
            return BadRequest(result.Messages);
            
        return CreatedAtAction(
            nameof(GetUser), 
            new { id = result.Value!.Value }, 
            result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserQuery(new UserId(id));
        var result = await Mediator.Send(query, cancellationToken);
        
        if (!result.Succeeded)
            return NotFound(result.Messages);
            
        return Ok(result.Value);
    }
}
```

## 🧪 Step 8: Test Your Service

### Build and Run

```bash
# Build the solution
dotnet build

# Run the API
cd UserService.Api
dotnet run
```

### Test with curl

```bash
# Create a user
curl -X POST https://localhost:7001/api/v1/users \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "+1234567890"
  }'

# Get the user
curl https://localhost:7001/api/v1/users/{user-id}
```

## 🎉 Congratulations!

You've successfully created your first microservice using the Baobab SharedKernel architecture! 

### What You've Accomplished

✅ **Clean Architecture**: Properly separated concerns across layers  
✅ **Domain-Driven Design**: Rich domain models with business logic  
✅ **CQRS**: Separated commands and queries  
✅ **Domain Events**: Event-driven architecture implemented  
✅ **Result Pattern**: Explicit error handling  
✅ **Validation**: Input validation with FluentValidation  
✅ **Repository Pattern**: Data access abstraction  
✅ **API Versioning**: Professional API design  

## 🚀 Next Steps

Now that you have a working service, explore these advanced topics:

1. **[Team Architecture Handoff Guide](./team-architecture-handoff-guide.md)** - Layer-by-layer reference for advanced domain modeling, complex use cases, external integrations, and API design
2. **[Patterns and Best Practices](./patterns-and-practices.md)** - Proven patterns with real examples

Ready to level up? Check out our [Practical Examples](./examples.md) for real-world scenarios!

---

**Need Help?** Check our [Troubleshooting Guide](./troubleshooting.md) or reach out to the community!