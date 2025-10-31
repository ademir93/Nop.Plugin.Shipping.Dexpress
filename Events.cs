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
using NUglify.Helpers;

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
        if (order == null)
            return;
        
        var orderNotes = await _orderService.GetOrderNotesByOrderIdAsync(order.Id);

        if (order.OrderStatusId != ReadyForShipmentStatusId)
            return;

        if (await _dexpressService.CheckIsOrderFlagByDexpress(orderNotes))
            return; // Already processed by Dexpress

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        if (orderItems == null || !orderItems.Any())
            return;
        
        var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();
        var productTasks = productIds.ToDictionary(id => id, id => _productService.GetProductByIdAsync(id));
        await Task.WhenAll(productTasks.Values);
        var products = productTasks.ToDictionary(kv => kv.Key, kv => kv.Value.Result);
        
        var productsByWarehouse = products.Values
            .Where(p => p != null)
            .GroupBy(p => p.WarehouseId > 0 ? p.WarehouseId : 0)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        
        productsByWarehouse.ForEach(shippments =>
        {
            _dexpressService.CreateDexpressShipmentAsync(
                order.Id,
                shippments.Key,
                orderItems.Where(oi => shippments.Value.Any(p => p.Id == oi.ProductId)).ToList(),
                shippments.Value
            ).ContinueWith(task =>
            {
                _eventPublisher.PublishAsync(new ShipmentCreatedEvent(task.Result));
            }).Wait();
        });
        
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = "A shipment has been added",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.Now
        });

        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            CreatedOnUtc = DateTime.Now,
            DisplayToCustomer = false,
            Note = "SentToDexpress",
        });
        
        var message = await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Added");
        _notificationService.SuccessNotification(message);
    }
}