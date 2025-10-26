using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public class DexpressSyncTask : IScheduleTask
{
    private readonly IDexpressService _dexpressService;
    
    public DexpressSyncTask(IDexpressService dexpressService)
    {
        _dexpressService = dexpressService;
    }
    
    public async Task ExecuteAsync()
    {
        await _dexpressService.SyncMunicipalityAsync();
        await _dexpressService.SyncTownsAsync();
        await _dexpressService.SyncStreetsAsync();
    }
}