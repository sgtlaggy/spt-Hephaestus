using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;


namespace Drebin.Patches;

public class GetAssortPatch : AbstractPatch
{
    protected static Mod mod = ServiceLocator.ServiceProvider.GetService<Mod>()!;
    protected static DataService data = ServiceLocator.ServiceProvider.GetService<DataService>()!;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderAssortHelper).GetMethod(nameof(TraderAssortHelper.GetAssort))!;
    }

    [PatchPrefix]
    protected static void RebuildAssort(MongoId sessionId, MongoId traderId)
    {
        var drebinId = data.GetBase().Id;

        if (traderId == drebinId)
        {
            mod.SetAssort(sessionId);
        }
    }
}
