# Domain Layer Organization Plan

## Current Structure Analysis

Based on the existing codebase, here's the current domain structure and a comprehensive plan for optimal organization:

### **Current Domain Structure**
```
SharedKernel.Domain/
├── Enums/
│   └── SortDirection.cs
├── Errors.cs
├── Lookups/
│   ├── LookupType.cs
│   └── LookupValue.cs
├── Notifications/
│   ├── Enums/
│   │   └── NotificationType.cs
│   ├── Notification.cs
│   └── Repositories/
│       └── INotificationRepository.cs
├── Primitives/
│   ├── AggregateRoot.cs
│   ├── DomainEvent.cs
│   ├── Entity.cs
│   ├── EntityExtra.cs
│   ├── IDomainEvent.cs
│   ├── ValueObject.cs
│   └── Factory/
│       └── EventFactory.cs
├── Requests/
│   ├── PaginatedRequest.cs
│   └── UploadRequest.cs
├── Results/
│   ├── Error.cs
│   ├── IResult.cs
│   ├── IValidationResult.cs
│   ├── PaginatedResult.cs
│   ├── Result.cs
│   ├── ResultT.cs
│   ├── ValidationResult.cs
│   └── ValidationResultT.cs
└── ValueObjects/
    ├── EmailAddress.cs
    ├── FirstName.cs
    ├── GhanaCardPersonalIdentificationNumber.cs
    ├── LastName.cs
    ├── Money.cs
    ├── OtherName.cs
    ├── PhoneNumber.cs
    └── UserId.cs
```

---

## **🎯 RECOMMENDED DOMAIN STRUCTURE**

### **1. Core Domain Primitives (Keep as Foundation)**
```
Primitives/
├── Base/
│   ├── Entity.cs                    ✅ Current
│   ├── EntityExtra.cs              ✅ Current (with audit support)
│   ├── AggregateRoot.cs            ✅ Current (with domain events)
│   ├── ValueObject.cs              ✅ Current
│   └── Enumeration.cs              📝 ADD (Smart enums pattern)
├── Events/
│   ├── IDomainEvent.cs             ✅ Current
│   ├── DomainEvent.cs              ✅ Current
│   ├── IntegrationEvent.cs         📝 ADD (Cross-service events)
│   └── Factory/
│       ├── EventFactory.cs        ✅ Current
│       └── IntegrationEventFactory.cs  📝 ADD
└── Contracts/
    ├── IAuditable.cs               📝 ADD (Audit interface)
    ├── ISoftDelete.cs              📝 ADD (Soft delete interface)
    ├── ITenant.cs                  📝 ADD (Multi-tenancy interface)
    └── IHasDomainEvents.cs         📝 ADD (Domain events interface)
```

### **2. Common Domain Concepts (Shared Business Logic)**
```
Common/
├── ValueObjects/
│   ├── Identity/
│   │   ├── UserId.cs               ✅ Current
│   │   ├── TenantId.cs             📝 ADD
│   │   ├── EntityId.cs             📝 ADD (Generic ID)
│   │   └── CorrelationId.cs        📝 ADD (Request tracing)
│   ├── PersonalInfo/
│   │   ├── FirstName.cs            ✅ Current
│   │   ├── LastName.cs             ✅ Current
│   │   ├── OtherName.cs            ✅ Current
│   │   ├── FullName.cs             📝 ADD (Composite name)
│   │   ├── EmailAddress.cs         ✅ Current
│   │   ├── PhoneNumber.cs          ✅ Current
│   │   └── DateOfBirth.cs          📝 ADD
│   ├── Financial/
│   │   ├── Money.cs                ✅ Current
│   │   ├── Currency.cs             📝 ADD (Enumeration-based)
│   │   ├── Percentage.cs           📝 ADD
│   │   └── ExchangeRate.cs         📝 ADD
│   ├── Geographic/
│   │   ├── Address.cs              📝 ADD
│   │   ├── Country.cs              📝 ADD (Smart enum)
│   │   ├── Region.cs               📝 ADD
│   │   └── Zone.cs                 📝 ADD (For zone-based auth)
│   ├── Identification/
│   │   ├── GhanaCardPersonalIdentificationNumber.cs  ✅ Current
│   │   ├── PassportNumber.cs       📝 ADD
│   │   ├── NationalId.cs           📝 ADD (Generic)
│   │   └── TaxId.cs                📝 ADD
│   └── Technical/
│       ├── Url.cs                  📝 ADD
│       ├── IpAddress.cs            📝 ADD
│       ├── UserAgent.cs            📝 ADD
│       └── ApiVersion.cs           📝 ADD
├── Enums/
│   ├── SortDirection.cs            ✅ Current
│   ├── Status.cs                   📝 ADD (Generic status)
│   ├── Priority.cs                 📝 ADD
│   └── ProcessingState.cs          📝 ADD
└── Specifications/
    ├── ISpecification.cs           📝 ADD (Specification interface)
    ├── BaseSpecification.cs        📝 ADD
    ├── AndSpecification.cs         📝 ADD
    ├── OrSpecification.cs          📝 ADD
    └── NotSpecification.cs         📝 ADD
```

### **3. Business Domain Aggregates**
```
Aggregates/
├── User/
│   ├── Entities/
│   │   ├── User.cs                 📝 ADD (User aggregate root)
│   │   ├── UserProfile.cs          📝 ADD
│   │   └── UserPreference.cs       📝 ADD
│   ├── ValueObjects/
│   │   ├── Username.cs             📝 ADD
│   │   ├── Password.cs             📝 ADD
│   │   └── UserRole.cs             📝 ADD (Smart enum)
│   ├── Events/
│   │   ├── UserCreatedDomainEvent.cs      📝 ADD
│   │   ├── UserUpdatedDomainEvent.cs      📝 ADD
│   │   └── UserDeactivatedDomainEvent.cs  📝 ADD
│   ├── Repositories/
│   │   └── IUserRepository.cs      📝 ADD
│   └── Specifications/
│       ├── ActiveUsersSpecification.cs    📝 ADD
│       └── UsersByRoleSpecification.cs    📝 ADD
├── Notification/
│   ├── Entities/
│   │   ├── Notification.cs         ✅ Current
│   │   ├── NotificationTemplate.cs 📝 ADD
│   │   └── NotificationLog.cs      📝 ADD
│   ├── ValueObjects/
│   │   ├── NotificationContent.cs  📝 ADD
│   │   └── DeliveryStatus.cs       📝 ADD
│   ├── Enums/
│   │   └── NotificationType.cs     ✅ Current
│   ├── Events/
│   │   ├── NotificationSentDomainEvent.cs     📝 ADD
│   │   └── NotificationFailedDomainEvent.cs   📝 ADD
│   ├── Repositories/
│   │   └── INotificationRepository.cs  ✅ Current
│   └── Services/
│       └── INotificationDomainService.cs  📝 ADD
└── Lookup/
    ├── Entities/
    │   ├── LookupType.cs           ✅ Current
    │   └── LookupValue.cs          ✅ Current
    ├── Events/
    │   ├── LookupCreatedDomainEvent.cs    📝 ADD
    │   └── LookupUpdatedDomainEvent.cs    📝 ADD
    ├── Repositories/
    │   └── ILookupRepository.cs    📝 ADD
    └── Services/
        └── ILookupDomainService.cs 📝 ADD
```

### **4. Cross-Cutting Domain Concerns**
```
CrossCutting/
├── Results/
│   ├── Abstractions/
│   │   ├── IResult.cs              ✅ Current
│   │   └── IValidationResult.cs    ✅ Current
│   ├── Core/
│   │   ├── Result.cs               ✅ Current
│   │   ├── ResultT.cs              ✅ Current
│   │   ├── Error.cs                ✅ Current
│   │   └── ErrorType.cs            📝 ADD (Enum for error types)
│   ├── Validation/
│   │   ├── ValidationResult.cs     ✅ Current
│   │   └── ValidationResultT.cs    ✅ Current
│   ├── Collections/
│   │   ├── PaginatedResult.cs      ✅ Current
│   │   └── PagedList.cs            📝 ADD
│   └── Extensions/
│       ├── ResultExtensions.cs     📝 ADD
│       └── ValidationExtensions.cs 📝 ADD
├── Errors/
│   ├── CommonErrors.cs             📝 REFACTOR from Errors.cs
│   ├── ValidationErrors.cs         📝 REFACTOR from Errors.cs
│   ├── BusinessRuleErrors.cs       📝 ADD
│   └── InfrastructureErrors.cs     📝 ADD
├── Exceptions/
│   ├── DomainException.cs          📝 ADD
│   ├── BusinessRuleViolationException.cs  📝 ADD
│   ├── InvalidValueObjectException.cs     📝 ADD
│   └── AggregateNotFoundException.cs      📝 ADD
└── Rules/
    ├── IBusinessRule.cs            📝 ADD
    ├── BusinessRuleValidator.cs    📝 ADD
    └── CompositeBusinessRule.cs    📝 ADD
```

### **5. Integration and External Contracts**
```
Contracts/
├── Requests/
│   ├── Queries/
│   │   ├── PaginatedRequest.cs     ✅ Current
│   │   ├── SearchRequest.cs        📝 ADD
│   │   └── FilterRequest.cs        📝 ADD
│   ├── Commands/
│   │   ├── CreateRequest.cs        📝 ADD
│   │   ├── UpdateRequest.cs        📝 ADD
│   │   └── DeleteRequest.cs        📝 ADD
│   └── Files/
│       ├── UploadRequest.cs        ✅ Current
│       ├── DownloadRequest.cs      📝 ADD
│       └── FileMetadata.cs         📝 ADD
├── Responses/
│   ├── ApiResponse.cs              📝 ADD
│   ├── PaginatedResponse.cs        📝 ADD
│   └── FileResponse.cs             📝 ADD
└── Integration/
    ├── Events/
    │   ├── IntegrationEvent.cs     📝 ADD
    │   └── IntegrationEventHandler.cs  📝 ADD
    └── Services/
        ├── IExternalService.cs     📝 ADD
        └── IIntegrationService.cs  📝 ADD
```

### **6. Configuration and Constants**
```
Configuration/
├── DomainConstants.cs              📝 ADD
├── ValidationConstants.cs          📝 ADD
├── BusinessConstants.cs            📝 ADD
└── RegionalConstants.cs            📝 ADD (Ghana-specific)
```

---

## **🚀 IMPLEMENTATION PLAN**

### **Phase 1: Reorganization (Immediate)**
1. **Move existing files to new structure**
   - Reorganize current files into proposed folders
   - Update namespaces accordingly
   - Update all references

2. **Refactor large files**
   - Break down `Errors.cs` into categorized error classes
   - Organize by domain concept

### **Phase 2: Enhancement (Short-term)**
3. **Add missing primitives**
   - Smart Enumerations base class
   - Integration events support
   - Domain interfaces (IAuditable, ISoftDelete, ITenant)

4. **Extend value objects**
   - Add missing common value objects
   - Implement composite value objects (FullName, Address)

### **Phase 3: Domain Expansion (Medium-term)**
5. **Build core aggregates**
   - User aggregate with events
   - Enhanced Notification aggregate
   - Lookup management aggregate

6. **Implement specifications**
   - Base specification pattern
   - Composite specifications (And, Or, Not)
   - Domain-specific specifications

### **Phase 4: Advanced Features (Long-term)**
7. **Business rules engine**
   - Rule validation framework
   - Composite business rules
   - Rule configuration

8. **Domain services**
   - Complex domain logic services
   - Cross-aggregate operations
   - Integration services

---

## **🎯 NAMING CONVENTIONS**

### **Folders**
- **PascalCase** for all folder names
- **Plural forms** for collections (ValueObjects, Entities, Events)
- **Singular forms** for concepts (Base, Core, Common)

### **Files**
- **PascalCase** with descriptive names
- **Suffixes**: `DomainEvent`, `ValueObject`, `Specification`, `Repository`, `Service`
- **Prefixes**: `I` for interfaces, `Base` for abstract classes

### **Namespaces**
```csharp
// Root namespace
Baobab.SharedKernel.Domain

// Core primitives
Baobab.SharedKernel.Domain.Primitives.Base
Baobab.SharedKernel.Domain.Primitives.Events

// Common concepts
Baobab.SharedKernel.Domain.Common.ValueObjects.Identity
Baobab.SharedKernel.Domain.Common.ValueObjects.Financial

// Business aggregates
Baobab.SharedKernel.Domain.Aggregates.User.Entities
Baobab.SharedKernel.Domain.Aggregates.User.Events

// Cross-cutting concerns
Baobab.SharedKernel.Domain.CrossCutting.Results.Core
Baobab.SharedKernel.Domain.CrossCutting.Errors
```

---

## **✅ BENEFITS OF THIS STRUCTURE**

### **1. Clear Separation of Concerns**
- Primitives contain reusable building blocks
- Common contains shared domain concepts
- Aggregates contain business-specific logic
- CrossCutting contains infrastructure concepts

### **2. Discoverability**
- Logical grouping makes features easy to find
- Consistent naming conventions
- Clear hierarchy from general to specific

### **3. Maintainability**
- Changes isolated to specific areas
- Dependencies flow in one direction
- Easy to extend without breaking existing code

### **4. Testability**
- Each concept can be tested independently
- Clear boundaries for unit testing
- Business logic separated from technical concerns

### **5. Team Development**
- Different teams can work on different aggregates
- Clear ownership boundaries
- Consistent patterns across the codebase

---

## **🎯 NEXT STEPS**

1. **Review and approve** this structure plan
2. **Start with Phase 1** - reorganize existing files
3. **Implement incrementally** - one phase at a time
4. **Update documentation** as changes are made
5. **Migrate existing code** gradually to new structure

This structure provides a solid foundation that can grow with your business needs while maintaining clean architecture principles.