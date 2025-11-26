using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Launcher;
using System.Reflection;


namespace Hephaestus;

public class GetAssortPatchUserLogin : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Specify the exact overload: SaveWeaponBuild(MongoId, PresetBuildActionRequestData)
        return typeof(LauncherController).GetMethod(
            nameof(LauncherController.Login))!;
    }

    
    [PatchPostfix]
    public static MongoId Postfix(MongoId sessionId)
    {
        try
        {
            var generateAssort = ServiceLocator.ServiceProvider.GetService<GenerateAssort>();
            var modHelper = ServiceLocator.ServiceProvider.GetService<ModHelper>();

            if (generateAssort == null || modHelper == null)
            {
                return sessionId;
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
                return sessionId;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetAssortPatch.Postfix exception: {ex}");
            return sessionId;
        }
        return sessionId;
    }
}
