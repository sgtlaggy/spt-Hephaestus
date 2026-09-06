using System.Reflection;
using FikaServer.Controllers;
using FikaServer.Models.Fika.Routes.Client;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;


namespace Drebin.Compat;

public class BuildsByProfileDict : Dictionary<MongoId, List<WeaponBuild>>;

[Injectable(InjectionType = InjectionType.Singleton)]
public class FikaHelper(
    IReadOnlyList<SptMod> _mods,
    ProfileDataService _profileDataService
) : IOnLoad
{
    public static string ProfileDataModKey = (new ModMetadata()).ModGuid;

    public Task OnLoad()
    {
        if (_mods.Any((mod) => mod.ModMetadata.ModGuid == "Fika"))
        {
            new ProfileDownloadPatch().Enable();
        }

        return Task.CompletedTask;
    }

    public BuildsByProfileDict GetOtherPlayerBuilds(MongoId sessionId)
    {
        return _profileDataService.GetProfileData<BuildsByProfileDict>(sessionId, ProfileDataModKey) ?? [];
    }
}

public class ProfileDownloadPatch : AbstractPatch
{
    protected static ProfileHelper _profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>()!;
    protected static JsonUtil _json = ServiceLocator.ServiceProvider.GetService<JsonUtil>()!;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientController).GetMethod(nameof(ClientController.HandleProfileDownload))!;
    }

    [PatchPostfix]
    protected static void SaveOtherBuildsToProfileModData(MongoId sessionId, DownloadProfileResponse? __result)
    {
        if (__result is null)
        {
            return;
        }

        BuildsByProfileDict otherProfileBuilds = [];

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

        __result.ModData ??= [];
        __result.ModData[FikaHelper.ProfileDataModKey] = _json.Serialize(otherProfileBuilds)!;
    }
}
