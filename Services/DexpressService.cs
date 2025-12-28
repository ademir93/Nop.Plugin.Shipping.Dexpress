using System.Globalization;
using System.Net.Http.Json;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.Dexpress.Domain;
using Nop.Plugin.Shipping.Dexpress.Models;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Models.Common;
using Nop.Web.Models.Customer;
using NUglify.Helpers;
using Nop.Web.Factories;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public class DexpressService : IDexpressService
{
    private readonly ILogger _logger;
    private readonly IRepository<Municipality> _municipalityRepository;
    private readonly IRepository<Town> _townRepository;
    private readonly IRepository<Street> _streetRepository;
    private readonly DexpressSettings _dexpressSettings;
    private readonly IRepository<Shipment> _shipmentRepository;
    private readonly IRepository<DexpressOrder> _orderRepository;
    private readonly IShipmentService _shipmentService;
    private readonly IRepository<DexAddress> _dexAddressRepository;
    private readonly IWorkContext _workContext;
    private readonly ICustomerService _customerService;
    private readonly IStoreMappingService _storeMappingService;
    private readonly ICountryService _countryService;
    private readonly IAddressModelFactory _addressModelFactory;
    private readonly AddressSettings _addressSettings;
    
    private string _dexApiUrl;
    private string _username;
    private string _password;
    private string _dateDex;
    private string _prefix;
    private int _rangeFrom;
    private int _rangeTo;
    
    public DexpressService(
        ILogger logger,
        IRepository<Municipality> municipalityRepository,
        IRepository<Town> townRepository,
        IRepository<Street> streetRepository,
        DexpressSettings dexpressSettings,
        IRepository<Shipment> shipmentRepository,
        IRepository<DexpressOrder> orderRepository,
        IShipmentService shipmentService,
        IRepository<DexAddress> dexAddressRepository,
        IWorkContext workContext,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        ICountryService countryService,
        IAddressModelFactory addressModelFactory,
        AddressSettings addressSettings
        )
    {
        _logger = logger;
        _municipalityRepository = municipalityRepository;
        _townRepository = townRepository;
        _streetRepository = streetRepository;
        _dexpressSettings = dexpressSettings;
        _shipmentRepository = shipmentRepository;
        _orderRepository = orderRepository;
        _shipmentService = shipmentService;
        _dexAddressRepository = dexAddressRepository;
        _workContext = workContext;
        _customerService = customerService;
        _storeMappingService = storeMappingService;
        _countryService = countryService;
        _addressModelFactory = addressModelFactory;
        _addressSettings = addressSettings;
        
        _dexApiUrl = _dexpressSettings.ApiUrl;
        _username = _dexpressSettings.Username;
        _password = _dexpressSettings.Password;
        _dateDex = _dexpressSettings.Datetime;
    }
    
    public virtual async Task<GetShippingOptionResponse> GetRatesAsync(GetShippingOptionRequest shippingOptionRequest)
    {
        var response = new GetShippingOptionResponse();

        //get regular rates
        var (shippingOptions, error) = await GetShippingOptionsAsync(shippingOptionRequest);
        response.ShippingOptions.Add(shippingOptions);

        if (!string.IsNullOrEmpty(error))
            response.Errors.Add(error);

        return response;
    }

    private async Task<(ShippingOption shippingOptions, string error)> GetShippingOptionsAsync(GetShippingOptionRequest shippingOptionRequest,
        bool saturdayDelivery = false)
    {
        try
        {
            var shippingOptions = new ShippingOption();
            shippingOptions.Name = DexpressDefaults.SystemName;
            shippingOptions.TransitDays = 1;
            shippingOptions.DisplayOrder = 0;
            shippingOptions.Description = string.Empty;
            shippingOptions.Rate = 0; //price of shippment

            return (shippingOptions, "");
        }
        catch (Exception exception)
        {
            //log errors
            var message = $"Error while getting Dex rates{Environment.NewLine}{exception.Message}";
            await _logger.ErrorAsync(message, exception, shippingOptionRequest.Customer);

            return (new ShippingOption(), message);
        }
    }

    public async Task<string> GetShippmentCodeAsync()
    {   
        string prefix = _dexpressSettings.Prefix ?? string.Empty;
        int rangeFrom = _dexpressSettings.RangeFrom;
        int rangeTo = _dexpressSettings.RangeTo;

        const int totalLength = 12;
        int numericLength = totalLength - prefix.Length;
        if (numericLength <= 0)
        {
            await _logger.ErrorAsync("Invalid prefix length for shipment code.");
            return null;
        }

        // get latest matching tracking number (string ordering works because numeric part is zero-padded)
        var lastTracking = _shipmentRepository.Table
            .Where(s => !string.IsNullOrEmpty(s.TrackingNumber)
                        && s.TrackingNumber.StartsWith(prefix)
                        && s.TrackingNumber.Length == totalLength)
            .OrderByDescending(s => s.TrackingNumber)
            .Select(s => s.TrackingNumber)
            .FirstOrDefault();

        int nextNumber;
        if (!string.IsNullOrEmpty(lastTracking))
        {
            var numericPartStr = lastTracking.Substring(prefix.Length);
            if (!int.TryParse(numericPartStr, NumberStyles.None, CultureInfo.InvariantCulture, out var lastNum))
            {
                await _logger.ErrorAsync("Failed to parse numeric part of last tracking number.");
                return null;
            }

            nextNumber = lastNum + 1;
        }
        else
        {
            nextNumber = rangeFrom;
        }

        if (nextNumber > rangeTo)
        {
            await _logger.ErrorAsync($"Shipment number exceeded configured range: {rangeTo}.");
            return null;
        }

        var numericPart = nextNumber.ToString(CultureInfo.InvariantCulture).PadLeft(numericLength, '0');
        return prefix + numericPart;
    }

    public async Task<Shipment> CreateDexpressShipmentAsync(int orderId, int warehouseId, List<OrderItem> orderItems, List<Product> products)
    {
        var dateTimeNow = DateTime.UtcNow;
        var trackingNumber = await GetShippmentCodeAsync();
        if (string.IsNullOrEmpty(trackingNumber))
            return null;

        var shipment = new Shipment
        {
            OrderId = orderId,
            TrackingNumber = trackingNumber,
            TotalWeight = null,
            AdminComment = null,
            CreatedOnUtc = dateTimeNow,
        };
        
        await _shipmentRepository.InsertAsync(shipment);
        
        if (shipment.Id <= 0)
            return null;
        
        var shipmentItems = products.Select(product => new ShipmentItem
        {
            ShipmentId = shipment.Id,
            OrderItemId = product.Id,
            WarehouseId = product.WarehouseId,
            Quantity = GetQuantityFromOrderItems(orderItems, product.Id)
        }).ToList();

        foreach (var item in shipmentItems)
        {
            await _shipmentService.InsertShipmentItemAsync(item);
        }

        return shipment;
    }
    
    public async Task<bool> CheckIsOrderFlagByDexpress(IList<OrderNote> orderNotes)
    {
        return await Task.FromResult(orderNotes.Any(note => note.Note.Contains("SentToDexpress")));
    }
    
    private int GetQuantityFromOrderItems(List<OrderItem> orderItems, int productId)
    {
        var totalQuantity = 0;
        orderItems.ForEach(orderItem =>
        {
            if (orderItem.ProductId == productId)
                totalQuantity += orderItem.Quantity;
        });

        return totalQuantity;
    }
    
    public async Task<DexpressOrder> PostDexpressOrderAsync(DexpressOrder dexpressOrder)
    {
        await _orderRepository.InsertAsync(dexpressOrder);
        return dexpressOrder;
    }
    
    public async Task<DexpressOrder> GetDexpressOrderAsync(int orderId)
    {
        return await _orderRepository.Table.FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<bool> SyncMunicipalityAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{this._username}:{this._password}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            var response = await httpClient.GetAsync($"{this._dexApiUrl}/data/municipalities?date={this._dateDex}");
            response.EnsureSuccessStatusCode();
        
            var municipalities = await response.Content.ReadFromJsonAsync<Municipality[]>();

            if (municipalities.Length > 0)
            {
                municipalities.ForEach(municipality =>
                {
                    var existingMunicipality = _municipalityRepository.Table.FirstOrDefault(m => m.MId == municipality.Id);
                    if (existingMunicipality == null)
                    {
                        municipality.MId = municipality.Id;
                        _municipalityRepository.Insert(municipality);
                    }
                    else
                    {
                        existingMunicipality.Name = municipality.Name;
                        existingMunicipality.PttNo = municipality.PttNo;
                        existingMunicipality.O = municipality.O;
                        _municipalityRepository.Update(existingMunicipality);
                    }
                });
            }
        
            return true;
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync($"Error syncing municipalities: {ex.Message}", ex);
            return false;
        }
    }
    
    public async Task<bool> SyncTownsAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{this._username}:{this._password}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            var response = await httpClient.GetAsync($"{this._dexApiUrl}/data/towns?date={this._dateDex}");
            response.EnsureSuccessStatusCode();
        
            var towns = await response.Content.ReadFromJsonAsync<Town[]>();

            if (towns.Length > 0)
            {
                towns.ForEach(town =>
                {
                    var existingTown = _townRepository.Table.FirstOrDefault(t => t.TId == town.Id);
                    if (existingTown == null)
                    {
                        town.TId = town.Id;
                        _townRepository.Insert(town);
                    }
                    else
                    {
                        existingTown.Name = town.Name;
                        existingTown.DName = town.DName;
                        existingTown.CentarId = town.CentarId;
                        existingTown.MId = town.MId;
                        existingTown.PttNo = town.PttNo;
                        existingTown.O = town.O;
                        existingTown.DeliveryDays = town.DeliveryDays;
                        existingTown.CutOffPickupTime = town.CutOffPickupTime;
                        _townRepository.Update(existingTown);
                    }
                });
            }
        
            return true;
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync($"Error syncing towns: {ex.Message}", ex);
            return false;
        }
    }
    
    public async Task<bool> SyncStreetsAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{this._username}:{this._password}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            var response = await httpClient.GetAsync($"{this._dexApiUrl}/data/streets?date={this._dateDex}");
            response.EnsureSuccessStatusCode();
        
            var streets = await response.Content.ReadFromJsonAsync<Street[]>();

            if (streets.Length > 0)
            {
                streets.ForEach(street =>
                {
                    var existingStreet = _streetRepository.Table.FirstOrDefault(t => t.TId == street.Id);
                    if (existingStreet == null)
                    {
                        street.SId = street.Id;
                        _streetRepository.Insert(street);
                    }
                    else
                    {
                        existingStreet.Name = street.Name;
                        existingStreet.TId = street.TId;
                        existingStreet.Del = street.Del;
                        _streetRepository.Update(existingStreet);
                    }
                });
            }
        
            return true;
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync($"Error syncing streets: {ex.Message}", ex);
            return false;
        }
    }
    
    public async Task<IList<Municipality>> GetAllMunicipalitiesAsync()
    {
        return await _municipalityRepository.Table.ToListAsync();
    }
    
    public async Task<IList<Town>> GetTownsByMunicipalityIdAsync(int municipalityId)
    {
        return await _townRepository.Table.Where(t => t.MId == municipalityId).ToListAsync();
    }
    
    public async Task<IList<Street>> GetStreetsByTownIdAsync(int townId)
    {
        return await _streetRepository.Table.Where(s => s.TId == townId).ToListAsync();
    }

    public async Task InsertDexAddressAsync(DexAddress address)
    {
        await _dexAddressRepository.InsertAsync(address);
    }

    public async Task<DexAddress> GetDexAddressByIdAsync(int addressId)
    {
        return await _dexAddressRepository.GetByIdAsync(addressId, cache => default, useShortTermCache: true);
    }

    public async Task UpdateDexAddressAsync(DexAddress address)
    {
        await _dexAddressRepository.UpdateAsync(address);
    }

    public async Task DeleteDexAddressAsync(DexAddress address)
    {
        await _dexAddressRepository.DeleteAsync(address);
    }
    
    public virtual async Task<DexCustomerAddressListModel> PrepareCustomerAddressListModelAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();

        var addresses = await (await _customerService.GetAddressesByCustomerIdAsync(customer.Id))
            //enabled for the current store
            .WhereAwait(async a => a.CountryId == null || await _storeMappingService.AuthorizeAsync(await _countryService.GetCountryByAddressAsync(a)))
            .ToListAsync();

        var model = new DexCustomerAddressListModel();
        foreach (var address in addresses)
        {
            var addressModel = new DexpressAddressModel();
            await _addressModelFactory.PrepareAddressModelAsync(addressModel,
                address: address,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesAsync((await _workContext.GetWorkingLanguageAsync()).Id));

            var dexAddress = await _dexAddressRepository.Table.FirstOrDefaultAsync(a => a.AddressId == address.Id);
            if (dexAddress != null)
            {
                addressModel.MunicipalityName = _municipalityRepository.Table.FirstOrDefault(m => m.MId == dexAddress.MunicipalityId)?.Name;
                addressModel.TownName = _townRepository.Table.FirstOrDefault(t => t.TId == dexAddress.TownId)?.Name;
                addressModel.StreetName = _streetRepository.Table.FirstOrDefault(s => s.SId == dexAddress.StreetId)?.Name;
            }
            
            model.Addresses.Add(addressModel);
        }
        return model;
    }
}