using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;


namespace Drebin.Compat;

[Injectable(InjectionType.Singleton)]
public class SharedWeaponBuildsHelper(IReadOnlyList<SptMod> _mods) : IOnLoad
{
    private bool modInstalled;

    public Task OnLoad()
    {
        if (_mods.Any((mod) => mod.ModMetadata.ModGuid == "wtf.archangel.sharedweaponbuilds"))
        {
            modInstalled = true;
        }

        return Task.CompletedTask;
    }

    public List<WeaponBuild> GetSharedBuilds()
    {
        if (!modInstalled)
        {
            return [];
        }

        return ExternalGetBuilds();
    }

    private List<WeaponBuild> ExternalGetBuilds()
    {
        var service = ServiceLocator.ServiceProvider.GetService<WeaponBuildService>();
        if (service is null)
        {
            return [];
        }

        return service.GetWeaponBuilds();
    }
}
