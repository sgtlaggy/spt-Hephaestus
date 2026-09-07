using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;


namespace DrebinFikaCompat;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class PatchLoader(
    ISptLogger<PatchLoader> logger,
    IReadOnlyList<SptMod> mods,
    IEnumerable<IRuntimePatch> patches
) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        bool drebinMissing = !ModInstalled(Constants.DrebinModGuid);
        bool fikaMissing = !ModInstalled(Constants.FikaModGuid);
        var uninstallMessage = "Consider uninstalling DrebinFikaCompat.";

        if (drebinMissing && fikaMissing)
        {
            logger.Warning($"Drebin and Fika are not installed. {uninstallMessage}");
        }
        else if (drebinMissing)
        {
            logger.Warning($"Drebin is not installed. {uninstallMessage}");
        }
        else if (fikaMissing)
        {
            logger.Warning($"Fika is not installed. {uninstallMessage}");
        }
        else
        {
            foreach (var patch in patches)
            {
                patch.Enable();
            }
        }

        return Task.CompletedTask;
    }

    public bool ModInstalled(string modGuid)
    {
        return mods.Any((mod) => (mod.ModMetadata.ModGuid == modGuid));
    }
}
