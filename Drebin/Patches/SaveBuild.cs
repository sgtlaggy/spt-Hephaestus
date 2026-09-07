using System.Reflection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Items;


namespace Drebin.Patches;

public class SaveBuildPatch : AbstractPatch
{
    protected static ItemBaseClassService _baseClassService = default!;

    public SaveBuildPatch(ItemBaseClassService baseClassService)
    {
        _baseClassService = baseClassService;
    }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(BuildController).GetMethod(nameof(BuildController.SaveWeaponBuild))!;
    }

    [PatchPrefix]
    protected static void CleanupItems(MongoId sessionId, PresetBuildActionRequestData request)
    {
        if (request.Items is null)
        {
            return;
        }

        // Remove ammo
        request.Items = request.Items.Where((item) => !_baseClassService.ItemHasBaseClass(item.Template, BaseClasses.AMMO));

        // Max durability
        foreach (var item in request.Items)
        {
            if (item.Upd?.Repairable is null)
            {
                continue;
            }

            item.Upd.Repairable.Durability = 100;
            item.Upd.Repairable.MaxDurability = 100;
        }
    }
}
