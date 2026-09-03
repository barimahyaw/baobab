# Practical Examples

## Real-World Scenarios with Baobab SharedKernel

This guide provides comprehensive, real-world examples showing how to implement common scenarios — whether in a microservice, a modular monolith, or a single service — using the SharedKernel architecture. Each example includes complete code implementations with explanations.

## 🛒 Example 1: E-Commerce Order Management

Let's build a complete order management system that demonstrates all the key patterns.

### Domain Layer Implementation

```csharp
// Domain/Entities/Order.cs
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();
    
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public Money Subtotal { get; private set; }
    public Money Tax { get; private set; }
    public Money Total { get; private set; }
    public OrderStatus Status { private set; get; }
    public ShippingAddress ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core constructor

    public Order(
        OrderId id, 
        CustomerId customerId, 
        ShippingAddress shippingAddress)
    {
        Id = id;
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        Subtotal = Money.Zero;
        Tax = Money.Zero;
        Total = Money.Zero;

        RaiseDomainEvent(new OrderCreatedDomainEvent(Id, CustomerId, CreatedAt));
    }

    public Result AddItem(ProductId productId, Money unitPrice, int quantity)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.BusinessRule("Order.CannotModifyNonDraft", "Cannot modify non-draft orders"));

        if (quantity <= 0)
            return Result.Fail(Error.Validation("Order.InvalidQuantity", "Quantity must be positive"));

        if (_items.Count >= 50)
            return Result.Fail(Error.BusinessRule("Order.MaxItems", "Cannot exceed 50 items per order"));

        // Check if item already exists
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var orderItem = OrderItem.Create(productId, unitPrice, quantity);
            _items.Add(orderItem);
        }

        RecalculateTotals();
        RaiseDomainEvent(new OrderItemAddedDomainEvent(Id, productId, quantity));
        
        return Result.Success();
    }

    public Result RemoveItem(ProductId productId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.BusinessRule("Order.CannotModifyNonDraft", "Cannot modify non-draft orders"));

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return Result.Fail(Error.NotFound("Order.ItemNotFound", "Item not found in order"));

        _items.Remove(item);
        RecalculateTotals();
        RaiseDomainEvent(new OrderItemRemovedDomainEvent(Id, productId));
        
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.BusinessRule("Order.AlreadyConfirmed", "Order is already confirmed"));

        if (!_items.Any())
            return Result.Fail(Error.BusinessRule("Order.EmptyOrder", "Cannot confirm empty order"));

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderConfirmedDomainEvent(Id, CustomerId, Total, Items.ToList()));
        
        return Result.Success();
    }

    public Result Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Fail(Error.BusinessRule("Order.CannotShip", "Can only ship confirmed orders"));

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new OrderShippedDomainEvent(Id, CustomerId, trackingNumber, ShippedAt.Value));
        
        return Result.Success();
    }

    private void RecalculateTotals()
    {
        Subtotal = _items.Sum(item => item.TotalPrice);
        Tax = Subtotal * 0.1m; // 10% tax rate
        Total = Subtotal + Tax;
    }
}

// Domain/Entities/OrderItem.cs
public class OrderItem : Entity
{
    public ProductId ProductId { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money TotalPrice { get; private set; }

    private OrderItem() { } // EF Core constructor

    private OrderItem(ProductId productId, Money unitPrice, int quantity)
    {
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        TotalPrice = unitPrice * quantity;
    }

    public static OrderItem Create(ProductId productId, Money unitPrice, int quantity)
    {
        return new OrderItem(productId, unitPrice, quantity);
    }

    public void UpdateQuantity(int newQuantity)
    {
        Quantity = newQuantity;
        TotalPrice = UnitPrice * Quantity;
    }
}

// Domain/ValueObjects/ShippingAddress.cs
public class ShippingAddress : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    private ShippingAddress(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public static Result<ShippingAddress> Create(string street, string city, string state, string postalCode, string country)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(street))
            errors.Add(Error.Validation("ShippingAddress.StreetRequired", "Street is required"));

        if (string.IsNullOrWhiteSpace(city))
            errors.Add(Error.Validation("ShippingAddress.CityRequired", "City is required"));

        if (string.IsNullOrWhiteSpace(postalCode))
            errors.Add(Error.Validation("ShippingAddress.PostalCodeRequired", "Postal code is required"));

        if (errors.Any())
            return Result<ShippingAddress>.Fail(errors.ToArray());

        return Result<ShippingAddress>.Success(
            new ShippingAddress(street, city, state, postalCode, country));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }
}

// Domain/Events/OrderConfirmedDomainEvent.cs
public record OrderConfirmedDomainEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    Money Total,
    List<OrderItem> Items) : DomainEvent;
```

### Application Layer Implementation

```csharp
// Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs
public record CreateOrderCommand(
    Guid CustomerId,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country) : ICommand<Result<OrderId>>;

// Application/Orders/Commands/CreateOrder/CreateOrderCommandHandler.cs
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Result<OrderId>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderId>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer: {CustomerId}", request.CustomerId);

        // Validate customer exists
        var customerId = new CustomerId(request.CustomerId);
        var customerExists = await _customerRepository.ExistsAsync(customerId, cancellationToken);
        if (!customerExists)
        {
            return Result<OrderId>.Fail(
                Error.NotFound("Customer.NotFound", "Customer not found"));
        }

        // Create shipping address
        var shippingAddressResult = ShippingAddress.Create(
            request.Street, request.City, request.State, request.PostalCode, request.Country);

        if (!shippingAddressResult.Succeeded)
            return Result<OrderId>.Fail(shippingAddressResult.Messages);

        // Create order
        var orderId = new OrderId(Guid.NewGuid());
        var order = new Order(orderId, customerId, shippingAddressResult.Value!);

        _orderRepository.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created successfully: {OrderId}", orderId);
        
        return Result<OrderId>.Success(orderId);
    }
}

// Application/Orders/Commands/AddOrderItem/AddOrderItemCommand.cs
public record AddOrderItemCommand(
    Guid OrderId,
    Guid ProductId,
    decimal UnitPrice,
    int Quantity) : ICommand<Result>;

// Application/Orders/Commands/AddOrderItem/AddOrderItemCommandHandler.cs
public class AddOrderItemCommandHandler : ICommandHandler<AddOrderItemCommand, Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPricingDomainService _pricingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOrderItemCommandHandler> _logger;

    public async Task<Result> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding item to order: {OrderId}, Product: {ProductId}", 
            request.OrderId, request.ProductId);

        // Get order
        var orderId = new OrderId(request.OrderId);
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            return Result.Fail(Error.NotFound("Order.NotFound", "Order not found"));

        // Validate product exists and get pricing
        var productId = new ProductId(request.ProductId);
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            return Result.Fail(Error.NotFound("Product.NotFound", "Product not found"));

        if (!product.IsAvailable)
            return Result.Fail(Error.BusinessRule("Product.Unavailable", "Product is not available"));

        // Calculate final price (considering discounts, promotions, etc.)
        var finalPrice = await _pricingService.CalculateDiscountedPriceAsync(
            productId, order.CustomerId, request.Quantity, cancellationToken);

        // Add item to order
        var addItemResult = order.AddItem(productId, finalPrice, request.Quantity);
        if (!addItemResult.Succeeded)
            return addItemResult;

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Item added to order successfully: {OrderId}", request.OrderId);
        
        return Result.Success();
    }
}

// Application/Orders/Queries/GetOrder/GetOrderQuery.cs
public record GetOrderQuery(Guid OrderId) : IQuery<Result<OrderDto>>;

// Application/Orders/Queries/GetOrder/OrderDto.cs
public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    ShippingAddressDto ShippingAddress,
    List<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public record ShippingAddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

// Application/Orders/Queries/GetOrder/GetOrderQueryHandler.cs
public class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, Result<OrderDto>>
{
    private readonly IOrderReadRepository _orderReadRepository;
    private readonly ICacheManager _cache;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    public async Task<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var cacheKey = $"order:{request.OrderId}";

        // Try cache first
        var cachedOrder = await _cache.GetAsync<OrderDto>(cacheKey, cancellationToken);
        if (cachedOrder != null)
        {
            _logger.LogDebug("Order found in cache: {OrderId}", request.OrderId);
            return Result<OrderDto>.Success(cachedOrder);
        }

        // Query database
        var order = await _orderReadRepository.GetByIdWithItemsAsync(orderId, cancellationToken);
        if (order == null)
            return Result<OrderDto>.Fail(Error.NotFound("Order.NotFound", "Order not found"));

        var orderDto = MapToDto(order);

        // Cache the result
        await _cache.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(15), cancellationToken);
        
        return Result<OrderDto>.Success(orderDto);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id.Value,
            order.CustomerId.Value,
            order.Status.ToString(),
            order.Subtotal.Amount,
            order.Tax.Amount,
            order.Total.Amount,
            new ShippingAddressDto(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country),
            order.Items.Select(item => new OrderItemDto(
                item.ProductId.Value,
                "", // Would be populated from product lookup
                item.UnitPrice.Amount,
                item.Quantity,
                item.TotalPrice.Amount)).ToList(),
            order.CreatedAt,
            order.ShippedAt,
            order.DeliveredAt);
    }
}
```

### Domain Event Handlers

```csharp
// Application/Orders/Events/OrderConfirmedDomainEventHandler.cs
public class OrderConfirmedDomainEventHandler : INotificationHandler<OrderConfirmedDomainEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<OrderConfirmedDomainEventHandler> _logger;

    public async Task Handle(OrderConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling order confirmed event: {OrderId}", notification.OrderId);

        try
        {
            // Reserve inventory
            foreach (var item in notification.Items)
            {
                await _inventoryService.ReserveInventoryAsync(
                    item.ProductId, item.Quantity, cancellationToken);
            }

            // Process payment
            var paymentResult = await _paymentService.ProcessPaymentAsync(
                notification.CustomerId, notification.Total, cancellationToken);

            if (!paymentResult.Succeeded)
            {
                _logger.LogError("Payment failed for order: {OrderId}", notification.OrderId);
                // Would publish OrderPaymentFailedEvent here
                return;
            }

            // Send confirmation email
            await _emailService.SendOrderConfirmationAsync(
                notification.CustomerId, notification.OrderId, cancellationToken);

            _logger.LogInformation("Order confirmed event processed successfully: {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order confirmed event: {OrderId}", notification.OrderId);
            // In a real system, you'd want to publish a failure event or retry
        }
    }
}

// Application/Orders/Events/OrderShippedDomainEventHandler.cs
public class OrderShippedDomainEventHandler : INotificationHandler<OrderShippedDomainEvent>
{
    private readonly IEmailNotificationService _emailService;
    private readonly ISMSNotificationService _smsService;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<OrderShippedDomainEventHandler> _logger;

    public async Task Handle(OrderShippedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling order shipped event: {OrderId}", notification.OrderId);

        try
        {
            var customer = await _customerRepository.GetByIdAsync(notification.CustomerId, cancellationToken);
            if (customer == null)
            {
                _logger.LogWarning("Customer not found for order shipped event: {CustomerId}", notification.CustomerId);
                return;
            }

            // Send shipping notification email
            await _emailService.SendShippingNotificationAsync(
                customer.Email,
                notification.OrderId,
                notification.TrackingNumber,
                cancellationToken);

            // Send SMS if customer has opted in
            if (customer.Phone != null && customer.SmsNotificationsEnabled)
            {
                await _smsService.SendAsync(
                    customer.Phone,
                    $"Your order {notification.OrderId.Value} has shipped! Tracking: {notification.TrackingNumber}",
                    cancellationToken);
            }

            _logger.LogInformation("Order shipped notifications sent: {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order shipped notifications: {OrderId}", notification.OrderId);
        }
    }
}
```

### API Controller

```csharp
// Presentation/Controllers/OrdersController.cs
[ApiVersion("1.0")]
public class OrdersController : BaseApiController<OrdersController>
{
    [HttpPost]
    [ProducesResponseType(typeof(OrderId), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command, 
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        
        if (!result.Succeeded)
            return BadRequest(result.Messages);
            
        return CreatedAtAction(
            nameof(GetOrder),
            new { id = result.Value!.Value },
            result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetOrderQuery(id);
        var result = await Mediator.Send(query, cancellationToken);
        
        if (!result.Succeeded)
            return NotFound(result.Messages);
            
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(
        Guid id,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddOrderItemCommand(id, request.ProductId, request.UnitPrice, request.Quantity);
        var result = await Mediator.Send(command, cancellationToken);
        
        if (!result.Succeeded)
            return BadRequest(result.Messages);
            
        return Ok();
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmOrderCommand(id);
        var result = await Mediator.Send(command, cancellationToken);
        
        if (!result.Succeeded)
            return BadRequest(result.Messages);
            
        return Ok();
    }

    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShipOrder(
        Guid id,
        [FromBody] ShipOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ShipOrderCommand(id, request.TrackingNumber);
        var result = await Mediator.Send(command, cancellationToken);
        
        if (!result.Succeeded)
            return BadRequest(result.Messages);
            
        return Ok();
    }
}

// Request DTOs
public record AddOrderItemRequest(Guid ProductId, decimal UnitPrice, int Quantity);
public record ShipOrderRequest(string TrackingNumber);
```

## 📊 Example 2: Analytics and Reporting Service

This example shows how to handle read-heavy workloads with CQRS.

### Read Model Optimization

```csharp
// Application/Analytics/Queries/GetSalesReport/SalesReportQuery.cs
public record GetSalesReportQuery(
    DateTime StartDate,
    DateTime EndDate,
    SalesReportGroupBy GroupBy = SalesReportGroupBy.Day,
    Guid? CustomerId = null,
    Guid? ProductId = null) : IQuery<Result<SalesReportDto>>;

public enum SalesReportGroupBy
{
    Day,
    Week,
    Month,
    Quarter,
    Year
}

// Application/Analytics/Queries/GetSalesReport/SalesReportDto.cs
public record SalesReportDto(
    DateTime StartDate,
    DateTime EndDate,
    SalesReportGroupBy GroupBy,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    List<SalesDataPointDto> DataPoints);

public record SalesDataPointDto(
    DateTime Date,
    decimal Revenue,
    int OrderCount,
    decimal AverageOrderValue);

// Application/Analytics/Queries/GetSalesReport/GetSalesReportQueryHandler.cs
public class GetSalesReportQueryHandler : IQueryHandler<GetSalesReportQuery, Result<SalesReportDto>>
{
    private readonly IAnalyticsReadRepository _analyticsRepository;
    private readonly ICacheManager _cache;
    private readonly ILogger<GetSalesReportQueryHandler> _logger;

    public async Task<Result<SalesReportDto>> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        // Cache key based on query parameters
        var cacheKey = $"sales-report:{request.StartDate:yyyy-MM-dd}:{request.EndDate:yyyy-MM-dd}:{request.GroupBy}:{request.CustomerId}:{request.ProductId}";
        
        // Try cache first (reports can be cached for longer periods)
        var cachedReport = await _cache.GetAsync<SalesReportDto>(cacheKey, cancellationToken);
        if (cachedReport != null)
        {
            _logger.LogDebug("Sales report found in cache");
            return Result<SalesReportDto>.Success(cachedReport);
        }

        // Generate report from database
        var dataPoints = await _analyticsRepository.GetSalesDataAsync(
            request.StartDate, 
            request.EndDate, 
            request.GroupBy,
            request.CustomerId,
            request.ProductId,
            cancellationToken);

        var report = new SalesReportDto(
            request.StartDate,
            request.EndDate,
            request.GroupBy,
            dataPoints.Sum(dp => dp.Revenue),
            dataPoints.Sum(dp => dp.OrderCount),
            dataPoints.Average(dp => dp.AverageOrderValue),
            dataPoints);

        // Cache for 1 hour (reports don't change frequently)
        await _cache.SetAsync(cacheKey, report, TimeSpan.FromHours(1), cancellationToken);

        return Result<SalesReportDto>.Success(report);
    }
}

// Infrastructure/Repositories/AnalyticsReadRepository.cs
public class AnalyticsReadRepository : IAnalyticsReadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<AnalyticsReadRepository> _logger;

    public async Task<List<SalesDataPointDto>> GetSalesDataAsync(
        DateTime startDate,
        DateTime endDate,
        SalesReportGroupBy groupBy,
        Guid? customerId,
        Guid? productId,
        CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = BuildSalesDataQuery(groupBy, customerId.HasValue, productId.HasValue);
        
        var parameters = new DynamicParameters();
        parameters.Add("startDate", startDate);
        parameters.Add("endDate", endDate);
        
        if (customerId.HasValue)
            parameters.Add("customerId", customerId);
            
        if (productId.HasValue)
            parameters.Add("productId", productId);

        var results = await connection.QueryAsync<SalesDataPointDto>(sql, parameters);
        
        return results.ToList();
    }

    private string BuildSalesDataQuery(SalesReportGroupBy groupBy, bool hasCustomerFilter, bool hasProductFilter)
    {
        var dateGrouping = groupBy switch
        {
            SalesReportGroupBy.Day => "DATE_TRUNC('day', o.CreatedAt)",
            SalesReportGroupBy.Week => "DATE_TRUNC('week', o.CreatedAt)",
            SalesReportGroupBy.Month => "DATE_TRUNC('month', o.CreatedAt)",
            SalesReportGroupBy.Quarter => "DATE_TRUNC('quarter', o.CreatedAt)",
            SalesReportGroupBy.Year => "DATE_TRUNC('year', o.CreatedAt)",
            _ => "DATE_TRUNC('day', o.CreatedAt)"
        };

        var sql = $@"
            SELECT 
                {dateGrouping} AS Date,
                SUM(o.Total) AS Revenue,
                COUNT(*) AS OrderCount,
                AVG(o.Total) AS AverageOrderValue
            FROM Orders o";

        if (hasProductFilter)
            sql += " INNER JOIN OrderItems oi ON o.Id = oi.OrderId";

        sql += @"
            WHERE o.CreatedAt >= @startDate 
            AND o.CreatedAt <= @endDate
            AND o.Status != 'Draft'";

        if (hasCustomerFilter)
            sql += " AND o.CustomerId = @customerId";

        if (hasProductFilter)
            sql += " AND oi.ProductId = @productId";

        sql += $@"
            GROUP BY {dateGrouping}
            ORDER BY Date";

        return sql;
    }
}
```

## 🔄 Example 3: Saga Pattern Implementation

Complex business processes that span multiple services.

```csharp
// Application/Sagas/OrderProcessingSaga.cs
public class OrderProcessingSaga : 
    INotificationHandler<OrderConfirmedDomainEvent>,
    INotificationHandler<PaymentProcessedDomainEvent>,
    INotificationHandler<InventoryReservedDomainEvent>,
    INotificationHandler<PaymentFailedDomainEvent>,
    INotificationHandler<InventoryReservationFailedDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<OrderProcessingSaga> _logger;

    public async Task Handle(OrderConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting order processing saga for order: {OrderId}", notification.OrderId);

        // Step 1: Reserve inventory
        try
        {
            foreach (var item in notification.Items)
            {
                await _inventoryService.ReserveInventoryAsync(
                    item.ProductId, item.Quantity, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory reservation failed for order: {OrderId}", notification.OrderId);
            await HandleSagaFailure(notification.OrderId, "Inventory reservation failed", cancellationToken);
        }
    }

    public async Task Handle(InventoryReservedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inventory reserved for order: {OrderId}, processing payment", notification.OrderId);

        // Step 2: Process payment
        try
        {
            await _paymentService.ProcessPaymentAsync(
                notification.CustomerId, notification.Amount, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment processing failed for order: {OrderId}", notification.OrderId);
            
            // Compensate: Release inventory
            await _inventoryService.ReleaseReservationAsync(notification.OrderId, cancellationToken);
            await HandleSagaFailure(notification.OrderId, "Payment processing failed", cancellationToken);
        }
    }

    public async Task Handle(PaymentProcessedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Payment processed for order: {OrderId}, saga completed successfully", notification.OrderId);
        
        // Saga completed successfully - order can now be shipped
        var order = await _orderRepository.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order != null)
        {
            // Move order to ready-to-ship status
            // This would trigger shipping workflows
        }
    }

    public async Task Handle(PaymentFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Payment failed for order: {OrderId}, starting compensation", notification.OrderId);
        
        // Compensate: Release inventory
        await _inventoryService.ReleaseReservationAsync(notification.OrderId, cancellationToken);
        await HandleSagaFailure(notification.OrderId, "Payment failed", cancellationToken);
    }

    public async Task Handle(InventoryReservationFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Inventory reservation failed for order: {OrderId}", notification.OrderId);
        await HandleSagaFailure(notification.OrderId, "Insufficient inventory", cancellationToken);
    }

    private async Task HandleSagaFailure(OrderId orderId, string reason, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order != null)
        {
            // Mark order as failed with reason
            order.MarkAsFailed(reason);
            _orderRepository.Update(order);
            
            // Notify customer of failure
            // Send email, create support ticket, etc.
        }
    }
}
```

## 🧪 Example 4: Comprehensive Testing Strategy

### Unit Testing

```csharp
// Tests/Domain/OrderTests.cs
public class OrderTests
{
    [Fact]
    public void AddItem_ValidItem_ShouldSucceed()
    {
        // Arrange
        var order = OrderTestData.CreateDraftOrder();
        var productId = new ProductId(Guid.NewGuid());
        var unitPrice = Money.FromDecimal(10.00m);
        const int quantity = 2;

        // Act
        var result = order.AddItem(productId, unitPrice, quantity);

        // Assert
        result.Should().Succeed();
        order.Items.Should().HaveCount(1);
        order.Items.First().ProductId.Should().Be(productId);
        order.Items.First().Quantity.Should().Be(quantity);
        order.Subtotal.Should().Be(Money.FromDecimal(20.00m));
    }

    [Fact]
    public void AddItem_OrderNotDraft_ShouldFail()
    {
        // Arrange
        var order = OrderTestData.CreateConfirmedOrder();
        var productId = new ProductId(Guid.NewGuid());
        var unitPrice = Money.FromDecimal(10.00m);

        // Act
        var result = order.AddItem(productId, unitPrice, 1);

        // Assert
        result.Should().Fail();
        result.Messages.Should().Contain(error => 
            error.Code == "Order.CannotModifyNonDraft");
    }

    [Fact]
    public void Confirm_ValidOrder_ShouldRaiseDomainEvent()
    {
        // Arrange
        var order = OrderTestData.CreateDraftOrderWithItems();
        order.ClearDomainEvents(); // Clear creation events

        // Act
        var result = order.Confirm();

        // Assert
        result.Should().Succeed();
        order.Status.Should().Be(OrderStatus.Confirmed);
        
        var domainEvent = order.GetDomainEvents()
            .Should().ContainSingle()
            .Which.Should().BeOfType<OrderConfirmedDomainEvent>().Subject;
        
        domainEvent.OrderId.Should().Be(order.Id);
        domainEvent.Total.Should().Be(order.Total);
    }
}
```

### Integration Testing

```csharp
// Tests/Integration/Orders/CreateOrderTests.cs
[Collection("Database")]
public class CreateOrderTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;

    public CreateOrderTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ShouldCreateOrder()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var request = new CreateOrderCommand(
            customer.Id.Value,
            "123 Test St",
            "Test City",
            "Test State",
            "12345",
            "Test Country");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var orderId = await response.Content.ReadFromJsonAsync<OrderId>();
        orderId.Should().NotBeNull();

        // Verify order was created in database
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = await dbContext.Orders.FindAsync(orderId);
        
        order.Should().NotBeNull();
        order!.CustomerId.Value.Should().Be(customer.Id.Value);
        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task CreateOrder_InvalidCustomer_ShouldReturnNotFound()
    {
        // Arrange
        var request = new CreateOrderCommand(
            Guid.NewGuid(), // Non-existent customer
            "123 Test St",
            "Test City",
            "Test State", 
            "12345",
            "Test Country");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errors = await response.Content.ReadFromJsonAsync<Error[]>();
        errors.Should().Contain(e => e.Code == "Customer.NotFound");
    }

    private async Task<Customer> SeedCustomerAsync()
    {
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var customer = new Customer(
            new CustomerId(Guid.NewGuid()),
            EmailAddress.Create("test@example.com").Value!,
            FirstName.Create("John").Value!,
            LastName.Create("Doe").Value!);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        
        return customer;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _client.Dispose();
    }
}

// Tests/Integration/CustomWebApplicationFactory.cs
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace database with test database
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Replace external services with mocks
            services.RemoveAll<IPaymentService>();
            services.AddScoped<IPaymentService, MockPaymentService>();

            services.RemoveAll<IInventoryService>();
            services.AddScoped<IInventoryService, MockInventoryService>();
        });
    }
}
```

### Behavior Testing

```csharp
// Tests/Behavior/OrderProcessingFeature.cs
[Binding]
public class OrderProcessingSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CustomWebApplicationFactory _factory;
    private Order _order;
    private Result _lastResult;

    public OrderProcessingSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _factory = new CustomWebApplicationFactory();
    }

    [Given(@"I have a draft order with (.*) items")]
    public void GivenIHaveADraftOrderWithItems(int itemCount)
    {
        _order = OrderTestData.CreateDraftOrder();
        
        for (int i = 0; i < itemCount; i++)
        {
            _order.AddItem(
                new ProductId(Guid.NewGuid()),
                Money.FromDecimal(10.00m),
                1);
        }
    }

    [When(@"I confirm the order")]
    public void WhenIConfirmTheOrder()
    {
        _lastResult = _order.Confirm();
    }

    [Then(@"the order should be confirmed")]
    public void ThenTheOrderShouldBeConfirmed()
    {
        _lastResult.Should().Succeed();
        _order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Then(@"an order confirmed event should be raised")]
    public void ThenAnOrderConfirmedEventShouldBeRaised()
    {
        _order.GetDomainEvents()
            .Should().Contain(e => e is OrderConfirmedDomainEvent);
    }
}

// Feature file: OrderProcessing.feature
Feature: Order Processing
    As a customer
    I want to place and confirm orders
    So that I can purchase products

Scenario: Confirming a draft order with items
    Given I have a draft order with 2 items
    When I confirm the order
    Then the order should be confirmed
    And an order confirmed event should be raised

Scenario: Cannot confirm empty order
    Given I have a draft order with 0 items
    When I confirm the order
    Then the operation should fail
    And the error should indicate "Cannot confirm empty order"
```

## 🚀 Next Steps

These examples demonstrate the power and flexibility of the SharedKernel architecture:

- **Rich Domain Models** with proper encapsulation and business logic
- **CQRS Implementation** with separate commands and queries
- **Event-Driven Architecture** for loose coupling
- **Saga Pattern** for complex business processes
- **Comprehensive Testing** at all levels

Ready to implement these patterns in your own services? Check out:

1. **[Team Architecture Handoff Guide](./team-architecture-handoff-guide.md)** - Domain modeling and CQRS in depth
2. **[Troubleshooting Guide](./troubleshooting.md)** - Common issues and solutions