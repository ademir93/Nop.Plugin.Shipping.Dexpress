using System.Net.Http.Json;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Plugin.Shipping.Dexpress.Domain;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using NUglify.Helpers;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public class DexpressService : IDexpressService
{
    private readonly ILogger _logger;
    private readonly IRepository<Municipality> _municipalityRepository;
    private readonly IRepository<Town> _townRepository;
    private readonly IRepository<Street> _streetRepository;
    private readonly DexpressSettings _dexpressSettings;
    
    private string _dexApiUrl;
    private string _username;
    private string _password;
    private string _dateDex;
    
    public DexpressService(
        ILogger logger,
        IRepository<Municipality> municipalityRepository,
        IRepository<Town> townRepository,
        IRepository<Street> streetRepository,
        DexpressSettings dexpressSettings
        )
    {
        _logger = logger;
        _municipalityRepository = municipalityRepository;
        _townRepository = townRepository;
        _streetRepository = streetRepository;
        _dexpressSettings = dexpressSettings;
        
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
}