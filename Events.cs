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
        var order = eventMessage.Entity;

        if (order.OrderStatusId != 20)
        {
            return;
        }

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

        var shipment = new Shipment
        {
            OrderId = order.Id,
            TrackingNumber = await _dexpressService.GetShippmentCodeAsync(),
            TotalWeight = null,
            AdminComment = null,
            CreatedOnUtc = DateTime.UtcNow
        };

        var shipmentItems = new List<ShipmentItem>();

        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
            var warehouseId = product.WarehouseId;

            var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (maxQtyToAdd <= 0)
                continue;


            //create a shipment item
            shipmentItems.Add(new ShipmentItem
            {
                OrderItemId = orderItem.Id,
                Quantity = orderItem.Quantity,
                WarehouseId = warehouseId
            });

            //if we have at least one item in the shipment, then save it
            if (shipmentItems.Any())
            {
                shipment.TotalWeight = null;
                await _shipmentService.InsertShipmentAsync(shipment);

                foreach (var shipmentItem in shipmentItems)
                {
                    shipmentItem.ShipmentId = shipment.Id;
                    await _shipmentService.InsertShipmentItemAsync(shipmentItem);
                }

                //add a note
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "A shipment has been added",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                await _eventPublisher.PublishAsync(new ShipmentCreatedEvent(shipment));

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Added"));
            }
        }
    }
}