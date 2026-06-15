using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Services;


namespace Drebin.Patches;

public class SaveBuildRemoveAmmoPatch : AbstractPatch
{
    protected static ItemBaseClassService _baseClassService = ServiceLocator.ServiceProvider.GetService<ItemBaseClassService>()!;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(BuildController).GetMethod(nameof(BuildController.SaveWeaponBuild))!;
    }

    [PatchPrefix]
    protected static void RemoveAmmo(MongoId sessionId, PresetBuildActionRequestData request)
    {
        if (request.Items is null)
        {
            return;
        }

        request.Items = request.Items.Where((item) => !_baseClassService.ItemHasBaseClass(item.Template, [BaseClasses.AMMO]));
    }
}
