using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Reflection;


namespace Hephaestus;

public class GetAssortPatch : AbstractPatch
{
    protected static GenerateAssort generateAssort = ServiceLocator.ServiceProvider.GetService<GenerateAssort>()!;
    protected static ModHelper modHelper = ServiceLocator.ServiceProvider.GetService<ModHelper>()!;
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderAssortHelper).GetMethod(nameof(TraderAssortHelper.GetAssort))!;
    }

    [PatchPrefix]
    protected static void RebuildAssort(MongoId traderId)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        Console.WriteLine(traderId);
        Console.WriteLine(traderBase.Id);
        if (traderId != traderBase.Id)
        {
            return;
        }

        generateAssort.buildAssort();
    }
}
