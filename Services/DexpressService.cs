using Nop.Core.Domain.Shipping;
using Nop.Services.Logging;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public class DexpressService : IDexpressService
{
    private readonly ILogger _logger;
    
    public DexpressService(ILogger logger)
    {
        _logger = logger;
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
            shippingOptions.Name = "Dex";
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
}