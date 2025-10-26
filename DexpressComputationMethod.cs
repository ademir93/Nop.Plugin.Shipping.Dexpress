using Nop.Core;
using Nop.Plugin.Shipping.Dexpress.Services;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Dexpress;

public class DexpressComputationMethod : BasePlugin, IShippingRateComputationMethod
{
    private readonly ILocalizationService _localizationService;
    private readonly IDexpressService _dexpressService;
    protected readonly IScheduleTaskService _scheduleTaskService;
    private readonly IWebHelper _webHelper;

    public DexpressComputationMethod(
        ILocalizationService localizationService,
        IDexpressService dexpressService,
        IScheduleTaskService scheduleTaskService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _dexpressService = dexpressService;
        _scheduleTaskService = scheduleTaskService;
        _webHelper = webHelper;
    }
    
    public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
    {
        if (getShippingOptionRequest == null)
            throw new ArgumentNullException(nameof(getShippingOptionRequest));

        if (!getShippingOptionRequest.Items?.Any() ?? true)
            return new GetShippingOptionResponse { Errors = new[] { "No shipment items" } };

        if (getShippingOptionRequest.ShippingAddress?.CountryId == null)
            return new GetShippingOptionResponse { Errors = new[] { "Shipping address is not set" } };

        return await _dexpressService.GetRatesAsync(getShippingOptionRequest);
    }
    
    public Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
    {
        return Task.FromResult<decimal?>(null);
    }
    
    public Task<IShipmentTracker> GetShipmentTrackerAsync()
    {
        return Task.FromResult<IShipmentTracker>(new DexpressShipmentTracker());
    }
    
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/Dexpress/Configure";
    }
    
    public override async Task InstallAsync()
    {
        await _scheduleTaskService.InsertTaskAsync(new()
        {
            Enabled = true,
            StopOnError = true,
            Name = DexpressDefaults.SynchronizationTask.Name,
            Type = DexpressDefaults.SynchronizationTask.Type,
            Seconds = DexpressDefaults.SynchronizationTask.Period,
        });
        
        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Shipping.Dex.Fields.Username"] = "Username",
            ["Plugins.Shipping.Dex.Fields.Username.Hint"] = "Specify Dex username.",
            ["Plugins.Shipping.Dex.Fields.Password"] = "Password",
            ["Plugins.Shipping.Dex.Fields.Password.Hint"] = "Specify Dex password.",
            ["Plugins.Shipping.Dex.Fields.ApiUrl"] = "Api Url",
            ["Plugins.Shipping.Dex.Fields.ApiUrl.Hint"] = "Api Url of Dex web service.",
            ["Plugins.Shipping.Dex.Fields.Datetime"] = "Datetime for request",
            ["Plugins.Shipping.Dex.Fields.Datetime.Hint"] = "Specify request datetime."
        });

        await base.InstallAsync();
    }
    
    public override async Task UninstallAsync()
    {
        await base.UninstallAsync();
    }

}