using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Reflection.Patching;
using Microsoft.Extensions.DependencyInjection;
using FikaServer.Controllers;


namespace Drebin.Patches;

public class SharedWeaponBuilds : Dictionary<MongoId, List<WeaponBuild>>;

[Injectable]
public class FikaHelper(
    DataService _data,
    IReadOnlyList<SptMod> _mods,
    ProfileDataService _profileDataService
) : IOnLoad
{
    public Task OnLoad()
    {
        if (!_data.GetConfig().SaveOtherProfileData)
        {
            return Task.CompletedTask;
        }

        if (_mods.Any((mod) => mod.ModMetadata.ModGuid == "Fika"))
        {
            new ProfileDownloadPatch().Enable();
        }

        return Task.CompletedTask;
    }

    public SharedWeaponBuilds GetOtherPlayerBuilds(MongoId sessionId)
    {
        return _profileDataService.GetProfileData<SharedWeaponBuilds>(sessionId, "drebin") ?? [];
    }
}

public class ProfileDownloadPatch : AbstractPatch
{
    protected static ProfileDataService _profileDataService = ServiceLocator.ServiceProvider.GetService<ProfileDataService>()!;
    protected static ProfileHelper _profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>()!;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientController).GetMethod(nameof(ClientController.HandleProfileDownload))!;
    }

    [PatchPrefix]
    protected static void BuildModData(MongoId sessionId)
    {
        SharedWeaponBuilds otherProfileBuilds = [];

        foreach (var (profileId, profile) in _profileHelper.GetProfiles())
        {
            if (profileId == sessionId)
            {
                continue;
            }

            if (profile.UserBuildData?.WeaponBuilds is null
                || profile.UserBuildData.WeaponBuilds.Count == 0)
            {
                continue;
            }

            otherProfileBuilds.Add(profileId, profile.UserBuildData.WeaponBuilds);
        }

        if (otherProfileBuilds.Count == 0)
        {
            return;
        }

        _profileDataService.SaveProfileData(sessionId, "drebin", otherProfileBuilds);
    }
}
