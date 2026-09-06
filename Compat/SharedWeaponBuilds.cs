using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Models;
using SharedWeaponBuilds.Server.Services;
using SharedWeaponBuilds.Server.WebSockets;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;


namespace Drebin.Compat;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class SharedWeaponBuildsHelper(IReadOnlyList<SptMod> _mods, FikaHelper _fikaHelper) : IOnLoad
{
    public Task OnLoad()
    {
        if (!_mods.Any((mod) => mod.ModMetadata.ModGuid == "wtf.archangel.sharedweaponbuilds"))
        {
            return Task.CompletedTask;
        }

        new ProfileUploadPatch().Enable();
        new LoadWeaponBuildsPatch().Enable();
        new SaveWeaponBuildPatch().Enable();
        new RemoveWeaponBuildPatch().Enable();

        return Task.CompletedTask;
    }
}

public class ProfileUploadPatch : AbstractPatch
{
    protected static ProfileHelper _profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>()!;
    protected static WeaponBuildService _buildService = ServiceLocator.ServiceProvider.GetService<WeaponBuildService>()!;

    protected override MethodBase? GetTargetMethod()
    {
        return typeof(SaveServer).GetMethod(nameof(SaveServer.AddProfile));
    }

    [PatchPostfix]
    protected static void UpdateBuildsFromProfile(SptProfile profileDetails)
    {
        var profileId = profileDetails.ProfileInfo!.ProfileId!.Value;
        var profileBuilds = profileDetails.UserBuildData?.WeaponBuilds ?? [];

        var allProfiles = _profileHelper.GetProfiles();
        var allProfileBuildIds = allProfiles.Values
            .SelectMany((profile) => (profile.UserBuildData?.WeaponBuilds ?? []))
            .Select((build) => (build.Id)).ToHashSet();

        foreach (var build in _buildService.GetWeaponBuilds())
        {
            if (!allProfileBuildIds.Contains(build.Id))
            {
                _buildService.RemoveWeaponBuild(profileId, build.Id);
            }
        }

        foreach (var build in profileBuilds)
        {
            _buildService.SaveWeaponBuild(profileId,
                new()
                {
                    Id = build.Id,
                    Name = build.Name,
                    Root = build.Root,
                    Items = build.Items
                });
        }
    }
}
public class LoadWeaponBuildsPatch : AbstractPatch
{
    protected static DatabaseService _db = ServiceLocator.ServiceProvider.GetService<DatabaseService>()!;
    protected static ProfileHelper _profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>()!;
    protected static ProfileDataService _profileDataService = ServiceLocator.ServiceProvider.GetService<ProfileDataService>()!;
    protected static FikaHelper _fikaHelper = ServiceLocator.ServiceProvider.GetService<FikaHelper>()!;
    protected static ISptLogger<LoadWeaponBuildsPatch> _logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<LoadWeaponBuildsPatch>>()!;

    protected override MethodBase? GetTargetMethod()
    {
        return typeof(WeaponBuildService).GetMethod(nameof(WeaponBuildService.LoadWeaponBuilds));
    }

    [PatchPrefix]
    protected static bool LoadWeaponBuilds(
        ConcurrentDictionary<MongoId, WeaponBuild> ___WeaponBuilds,
        ref Task __result
    )
    {
        var itemTemplates = _db.GetItems();
        ___WeaponBuilds.Clear();

        var allProfiles = _profileHelper.GetProfiles();
        var allProfileBuilds = allProfiles.Values
            .SelectMany((profile) => (profile.UserBuildData?.WeaponBuilds ?? []))
            .ToList();
        foreach (var profileId in allProfiles.Keys)
        {
            var profileData = _profileDataService.GetProfileData<List<WeaponBuild>>(profileId, FikaHelper.ProfileDataModKey);
            if (profileData is null)
            {
                continue;
            }

            allProfileBuilds.AddRange(profileData);
        }

        foreach (var build in allProfileBuilds)
        {
            if (build.Items is null)
            {
                _logger.Warning($"Build {build.Id} .Items is null.");
                continue;
            }

            HashSet<MongoId> missingTemplates = [];
            foreach (var item in build.Items)
            {
                if (!itemTemplates.ContainsKey(item.Template))
                {
                    missingTemplates.Add(item.Template);
                }
            }
            if (missingTemplates.Count > 0)
            {
                _logger.Warning($"Build {build.Id} contains missing templates: {String.Join(", ", missingTemplates)}");
                continue;
            }

            if (!___WeaponBuilds.TryAdd(build.Id, build))
            {
                _logger.Warning($"Build {build.Id} already cached.");
            }
        }

        __result = Task.CompletedTask;
        return false;
    }
}
public class SaveWeaponBuildPatch : AbstractPatch
{
    protected static ISptLogger<SaveWeaponBuildPatch> _logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<SaveWeaponBuildPatch>>()!;

    protected override MethodBase? GetTargetMethod()
    {
        return typeof(WeaponBuildService).GetMethod(nameof(WeaponBuildService.SaveWeaponBuild));
    }

    [PatchPrefix]
    protected static bool SaveWeaponBuild(
        ref Task __result,
        MongoId sessionId,
        PresetBuildActionRequestData request,
        WeaponBuildsWebSocket ___weaponBuildsWebsocket,
        ConcurrentDictionary<MongoId, WeaponBuild> ___WeaponBuilds
    )
    {
        if (request.Items is null)
        {
            _logger.Error($"SaveWeaponBuild request.Items is null for build {request.Id}.");
            __result = Task.CompletedTask;
            return false;
        }

        WeaponBuild build = new()
        {
            Id = request.Id,
            Name = request.Name,
            Root = request.Root,
            Items = request.Items.ToList(),
        };

        ___WeaponBuilds.AddOrUpdate(build.Id, build, (_, _) => (build));

        __result = ___weaponBuildsWebsocket.BroadcastAsync(sessionId,
            new()
            {
                BuildId = build.Id,
                UpdatedWeaponBuild = build,
                IsDeleted = false,
            });
        return false;
    }
}
public class RemoveWeaponBuildPatch : AbstractPatch
{
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(WeaponBuildService).GetMethod(nameof(WeaponBuildService.RemoveWeaponBuild));
    }

    [PatchPrefix]
    protected static bool RemoveWeaponBuild(
        ref Task __result,
        MongoId sessionId,
        MongoId buildId,
        WeaponBuildsWebSocket ___weaponBuildsWebsocket,
        ConcurrentDictionary<MongoId, WeaponBuild> ___WeaponBuilds
    )
    {
        ___WeaponBuilds.TryRemove(buildId, out _);

        __result = ___weaponBuildsWebsocket.BroadcastAsync(sessionId,
            new()
            {
                BuildId = buildId,
                IsDeleted = true
            }
        );

        return false;
    }
}
