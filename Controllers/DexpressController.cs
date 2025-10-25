using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Shipping.Dexpress.Models;
using Nop.Plugin.Shipping.Dexpress.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.Dexpress.Controllers;

[AutoValidateAntiforgeryToken]
[AuthorizeAdmin] //confirms access to the admin panel
[Area(AreaNames.ADMIN)] //specifies the area containing a controller or action
public class DexpressController : BasePluginController
{
    private readonly ILocalizationService _localizationService;
    private readonly IDexpressService _dexpressService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly INotificationService _notificationService;
    private readonly DexpressSettings _dexpressSettings;

    public DexpressController(
        ILocalizationService localizationService,
        IDexpressService dexpressService,
        IPermissionService permissionService,
        ISettingService settingService,
        INotificationService notificationService,
        DexpressSettings dexpressSettings)
    {
        _localizationService = localizationService;
        _dexpressService = dexpressService;
        _permissionService = permissionService;
        _settingService = settingService;
        _notificationService = notificationService;
        _dexpressSettings = dexpressSettings;
    }
    
    [HttpGet]
    [CheckPermission(StandardPermission.Configuration.MANAGE_SHIPPING_SETTINGS)]
    public async Task<IActionResult> Configure()
    {
        var model = new ConfigurationModel
        {
            Username = _dexpressSettings.Username,
            Password = _dexpressSettings.Password,
            ApiUrl = _dexpressSettings.ApiUrl,
            Datetime = _dexpressSettings.Datetime
        };

        return View("~/Plugins/Shipping.Dexpress/Views/Configure.cshtml", model);
    }
    
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_SHIPPING_SETTINGS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        //if (!ModelState.IsValid)
        //    return await Configure();

        _dexpressSettings.Username = model.Username;
        _dexpressSettings.Password = model.Password;
        _dexpressSettings.ApiUrl = model.ApiUrl;
        _dexpressSettings.Datetime = model.Datetime;

        await _settingService.SaveSettingAsync(_dexpressSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }
}