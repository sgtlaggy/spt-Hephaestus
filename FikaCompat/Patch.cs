using System.Reflection;
using FikaServer.Controllers;
using FikaServer.Models.Fika.Routes.Client;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Services.Modding;
using SPTarkov.Server.Core.Utils;


namespace DrebinFikaCompat;

public class BuildsByProfileDict : Dictionary<MongoId, List<WeaponBuild>>;

[Injectable]
public class FikaProfileDataPatch : AbstractPatch
{
    protected static ProfileDataService _profileDataService = default!;
    protected static ProfileHelper _profileHelper = default!;
    protected static JsonUtil _json = default!;

    private bool FikaInstalled = false;

    public FikaProfileDataPatch(
        ProfileDataService profileDataService,
        ProfileHelper profileHelper,
        JsonUtil json
    )
    {
        _profileDataService = profileDataService;
        _profileHelper = profileHelper;
        _json = json;
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
        __result.ModData[Constants.DrebinModGuid] = _json.Serialize(otherProfileBuilds)!;
    }
}
