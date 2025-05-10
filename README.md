# Domain Driven Design, Event Driven with Clean Architecture

### Overview
Welcome to the .NET 8 C# microservices project, leveraging various industry best practices and design patterns to ensure a robust, scalable, and maintainable solution. This project embraces technologies like Entity Framework Core 8, C# 12, Clean Architecture, Domain-Driven Design (DDD), Event-Driven Design, and follows industry standards such as CQRS, Specification Pattern, SOLID principles, Gang of Four Design Patterns, Outbox Pattern, and more.


### Project Structure
Follows Clean Architecture and Domain-Driven Design principles, separating bounded contexts into individual microservices. This ensures a clear separation of concerns, scalability, and maintainability.

### Key Technologies
* .NET 8
* C# 12
* Entity Framework Core 8
* MediatR and FluentValidation for validation
* Polly for fault handling
* Moq, Fluent Assertion, and xUnit for unit testing
* Dapper for faster SQL queries
* Marten Document Database with PostgreSQL for NoSQL requirements
* RabbitMQ and MassTransit for message queue
* Redis for distributed caching
* Docker, Docker Compose, and Kubernetes for containerization and orchestration

### Design Patterns and Practices
* CQRS (Command Query Responsibility Segregation)
* Specification Pattern
* SOLID Principles
* Gang of Four Design Patterns
* Outbox Pattern
* Domain Event Pattern
* DDD CAP Theorem
* Unit of Work pattern with EF Core
* Smart Enums with rich behavior
* Materialized View for speeding up queries
* Event-Driven Architecture
* API Idempotency to prevent duplicate requests

### Testing
Unit testing is done using Moq, Fluent Assertion, and xUnit. This ensures the reliability and correctness of the codebase.

### Data Access
EF Core is used for relational databases, while Dapper is employed for faster SQL queries. Marten Document Database with PostgreSQL is chosen for microservices requiring a NoSQL solution.

### Observability
Logging is treated as a cross-cutting concern and is addressed using MediatR. Message queues (RabbitMQ and MassTransit) enhance observability by facilitating asynchronous communication.

### Idempotency and Saga
Idempotency is ensured through API design, preventing the execution of duplicate requests. The system employs Domain Events to build a decoupled architecture that scales. Sagas are implemented to manage long-running processes.

### Deployment
The project is containerized using Docker, orchestrated with Docker Compose, and can be scaled using Kubernetes for efficient deployment and management.

### Additional Best Practices
* Follow principles of OOP
* Use Value Objects to overcome Primitive Obsession
* Aggregate Root design for maintaining consistency
* Handle duplicate messages and API calls with idempotent strategies
* Implement distributed caching with Redis for performance optimization

### API Gateway
Introducing the API Gateway using YARP (Yet Another Reverse Proxy). YARP provides a flexible and efficient way to route, load balance, and secure traffic to the microservices. 
It acts as the entry point to the system, managing requests and distributing them to the appropriate microservices. This enhances the system's scalability, resilience, and security.

Feel free to explore the codebase and contribute to this well-architected, industry-standard microservices project. Happy coding!

### References
* Clean Architecture Folder Structure
https://www.milanjovanovic.tech/blog/clean-architecture-folder-structure?utm_source=LinkedIn&utm_medium=social&utm_campaign=25.12.2023
* CAP Theorem
https://www.educative.io/blog/what-is-cap-theorem
* Important Component of System Design and Architecture
https://www.educative.io/blog/components-of-system-design

