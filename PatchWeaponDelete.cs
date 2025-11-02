using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Reflection;


namespace Hephaestus;

public class GetAssortPatchWeaponDelete : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Specify the exact overload: (MongoId, PresetBuildActionRequestData)
        return typeof(BuildController).GetMethod(
            nameof(BuildController.RemoveBuild))!;
    }


    [PatchPostfix]
    public static void Postfix(object __instance, SPTarkov.Server.Core.Models.Common.MongoId sessionId, SPTarkov.Server.Core.Models.Eft.PresetBuild.PresetBuildActionRequestData request)
    {
        try
        {
            var generateAssort = ServiceLocator.ServiceProvider.GetService<GenerateAssort>();
            var modHelper = ServiceLocator.ServiceProvider.GetService<ModHelper>();

            if (generateAssort == null || modHelper == null)
            {
                return;
            }

            var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");

            Task.Run(() =>
            {
                try
                {
                    generateAssort.buildAssort();
                }
                catch (Exception exInner)
                {
                    Console.WriteLine($"GetAssortPatch.buildAssort exception: {exInner}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetAssortPatch.Postfix exception: {ex}");
        }
    }
}
