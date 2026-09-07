using System.Reflection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Models.Common;


namespace Drebin.Patches;

public class GetAssortPatch : AbstractPatch
{
    protected static Mod _mod = default!;
    protected static DataService _data = default!;

    public GetAssortPatch(
        Mod mod,
        DataService data
    )
    {
        _mod = mod;
        _data = data;
    }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderAssortHelper).GetMethod(nameof(TraderAssortHelper.GetAssort))!;
    }

    [PatchPrefix]
    protected static void RebuildAssort(MongoId sessionId, MongoId traderId)
    {
        var drebinId = _data.GetBase().Id;

        if (traderId == drebinId)
        {
            _mod.SetAssort(sessionId);
        }
    }
}
