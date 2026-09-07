using System.Reflection;
using FikaServer.Controllers;
using FikaServer.Models.Fika.Routes.Client;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Modding;
using SPTarkov.Server.Core.Utils;


namespace Drebin.Compat;

public class BuildsByProfileDict : Dictionary<MongoId, List<WeaponBuild>>;

[Injectable(InjectionType = InjectionType.Singleton)]
public class ProfileDataPatch : AbstractPatch
{
    public static string ProfileDataModKey = (new ModMetadata()).ModGuid;

    protected static ProfileHelper _profileHelper = default!;
    protected static JsonUtil _json = default!;
    protected static IReadOnlyList<SptMod> _mods = default!;
    protected static ProfileDataService _profileDataService = default!;

    public ProfileDataPatch(
        ProfileHelper profileHelper,
        JsonUtil json,
        IReadOnlyList<SptMod> mods,
        ProfileDataService profileDataService
    )
    {
        _profileHelper = profileHelper;
        _json = json;
        _mods = mods;
        _profileDataService = profileDataService;
    }

    public async Task<BuildsByProfileDict> GetOtherPlayerBuilds(MongoId sessionId)
    {
        var builds = await _profileDataService.GetProfileDataAsync<BuildsByProfileDict>(sessionId, ProfileDataModKey);
        return builds ?? [];
    }

    public new void Enable()
    {
        if (_mods.Any((mod) => (mod.ModMetadata.ModGuid == "Fika")))
        {
            base.Enable();
        }
    }

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
        __result.ModData[ProfileDataModKey] = _json.Serialize(otherProfileBuilds)!;
    }
}
