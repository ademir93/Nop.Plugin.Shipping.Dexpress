using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Plugin.Shipping.Dexpress.Services;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.Dexpress;

public class EventConsumer : IConsumer<EntityUpdatedEvent<Order>>
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly IDexpressService _dexpressService;
    
    public EventConsumer(
        IOrderService orderService, 
        IProductService productService, 
        IShipmentService shipmentService, 
        IEventPublisher eventPublisher,
        INotificationService notificationService,
        ILocalizationService localizationService,
        IDexpressService dexpressService
    )
    {
        _orderService = orderService;
        _productService = productService;
        _shipmentService = shipmentService;
        _eventPublisher = eventPublisher;
        _notificationService = notificationService;
        _localizationService = localizationService;
        _dexpressService = dexpressService;
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<Order> eventMessage)
    {
        const int ReadyForShipmentStatusId = 30; // status 'Complete or Ready for Shipment' ID

        var order = eventMessage.Entity;
        if (order == null || order.OrderStatusId != ReadyForShipmentStatusId)
            return;

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        if (orderItems == null || !orderItems.Any())
            return;

        var now = DateTime.UtcNow;
        var trackingNumber = await _dexpressService.GetShippmentCodeAsync();

        var shipment = new Shipment
        {
            OrderId = order.Id,
            TrackingNumber = trackingNumber,
            TotalWeight = null,
            AdminComment = null,
            CreatedOnUtc = now
        };

        await _shipmentService.InsertShipmentAsync(shipment);
        if (shipment.Id <= 0)
            return;

        var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();
        var productTasks = productIds.ToDictionary(id => id, id => _productService.GetProductByIdAsync(id));
        await Task.WhenAll(productTasks.Values);
        var products = productTasks.ToDictionary(kv => kv.Key, kv => kv.Value.Result);

        var shipmentItems = new List<ShipmentItem>();
        foreach (var orderItem in orderItems)
        {
            if (!products.TryGetValue(orderItem.ProductId, out var product) || product == null)
                continue;

            var warehouseId = product.WarehouseId;
            var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (maxQtyToAdd <= 0)
                continue;

            var quantity = Math.Min(orderItem.Quantity, maxQtyToAdd);

            shipmentItems.Add(new ShipmentItem
            {
                ShipmentId = shipment.Id,
                OrderItemId = orderItem.Id,
                Quantity = quantity,
                WarehouseId = warehouseId
            });
        }

        if (!shipmentItems.Any())
            return;

        // insert shipment items in parallel
        var insertTasks = shipmentItems.Select(si => _shipmentService.InsertShipmentItemAsync(si));
        await Task.WhenAll(insertTasks);

        // add order note, publish event and notify admin
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = "A shipment has been added",
            DisplayToCustomer = false,
            CreatedOnUtc = now
        });

        await _eventPublisher.PublishAsync(new ShipmentCreatedEvent(shipment));
        var msg = await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Added");
        _notificationService.SuccessNotification(msg);
    }
}