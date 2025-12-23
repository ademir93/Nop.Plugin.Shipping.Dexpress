using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Http;
using Nop.Plugin.Shipping.Dexpress.Domain;
using Nop.Plugin.Shipping.Dexpress.Models;
using Nop.Plugin.Shipping.Dexpress.Services;
using Nop.Services.Attributes;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Models.Customer;

namespace Nop.Plugin.Shipping.Dexpress.Controllers;

public class DexpressCustomerController : BasePublicController
{
    protected readonly AddressSettings _addressSettings;
    protected readonly ICustomerService _customerService;
    protected readonly IWorkContext _workContext;
    protected readonly IAddressModelFactory _addressModelFactory;
    protected readonly ICountryService _countryService;
    protected readonly IAttributeParser<AddressAttribute, AddressAttributeValue> _addressAttributeParser;
    protected readonly IAddressService _addressService;
    protected readonly INotificationService _notificationService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IDexpressService _dexpressService;

    public DexpressCustomerController(
        AddressSettings addressSettings,
        ICustomerService customerService,
        IWorkContext workContext,
        IAddressModelFactory addressModelFactory,
        ICountryService countryService,
        IAttributeParser<AddressAttribute, AddressAttributeValue> addressAttributeParser,
        IAddressService addressService,
        INotificationService notificationService,
        ILocalizationService localizationService,
        IDexpressService dexpressService)
    {
        _addressSettings = addressSettings;
        _customerService = customerService;
        _workContext = workContext;
        _addressModelFactory = addressModelFactory;
        _countryService = countryService;
        _addressAttributeParser = addressAttributeParser;
        _addressService = addressService;
        _notificationService = notificationService;
        _localizationService = localizationService;
        _dexpressService = dexpressService;
    }
    
    public virtual async Task<IActionResult> AddressAdd()
    {
        if (!await _customerService.IsRegisteredAsync(await _workContext.GetCurrentCustomerAsync()))
            return Challenge();

        var model = new DexpressAddress();
        await _addressModelFactory.PrepareAddressModelAsync(model.Address,
            address: null,
            excludeProperties: false,
            addressSettings: _addressSettings);

        model.Address.CountryId = 198; //Serbia
        model.Address.StateProvinceId = 1433; //Serbia

        model.Address.Municipalities = await _dexpressService.GetAllMunicipalitiesAsync();
        
        return View("~/Plugins/Shipping.Dexpress/Views/Customer/AddressAdd.cshtml", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> AddressAdd(DexpressAddress model, IFormCollection form)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        //custom address attributes
        var customAttributes = await _addressAttributeParser.ParseCustomAttributesAsync(form, NopCommonDefaults.AddressAttributeControlName);
        var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
        foreach (var error in customAttributeWarnings)
        {
            ModelState.AddModelError("", error);
        }

        if (ModelState.IsValid)
        {
            var address = model.Address.ToEntity();
            address.CustomAttributes = customAttributes;
            address.CreatedOnUtc = DateTime.UtcNow;
            //some validation
            if (address.CountryId == 0)
                address.CountryId = null;
            if (address.StateProvinceId == 0)
                address.StateProvinceId = null;


            await _addressService.InsertAddressAsync(address);

            await _dexpressService.InsertDexAddressAsync(new DexAddress
            {
                AddressId = address.Id,
                MunicipalityId = model.Address.MunicipalityId,
                TownId = model.Address.TownId,
                StreetId = model.Address.StreetId,
            });

            await _customerService.InsertCustomerAddressAsync(customer, address);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Account.CustomerAddresses.Added"));

            return RedirectToAction("AddressAdd", "DexpressCustomer");
        }

        //If we got this far, something failed, redisplay form
        await _addressModelFactory.PrepareAddressModelAsync(model.Address,
            address: null,
            excludeProperties: true,
            addressSettings: _addressSettings,
            loadCountries: async () => await _countryService.GetAllCountriesAsync((await _workContext.GetWorkingLanguageAsync()).Id),
            overrideAttributesXml: customAttributes);

        return View("~/Plugins/Shipping.Dexpress/Views/Customer/AddressAdd.cshtml", model);
    }
    
    [HttpPost]
    public virtual async Task<IActionResult> GetTownsByMunicipalityId(int municipalityId)
    {
        var towns = await _dexpressService.GetTownsByMunicipalityIdAsync(municipalityId);
        var result = towns.Select(x => new { id = x.TId, name = x.Name }).ToList();
        return Json(result);
    }
    
    [HttpPost]
    public virtual async Task<IActionResult> GetStreetsByTownId(int townId)
    {
        var streets = await _dexpressService.GetStreetsByTownIdAsync(townId);
        var result = streets.Select(x => new { id = x.SId, name = x.Name }).ToList();
        return Json(result);
    }
    
}