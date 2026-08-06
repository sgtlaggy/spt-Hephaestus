using Drebin.Compat;
using Drebin.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;


namespace Drebin;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 1, InjectionType = InjectionType.Singleton)]
public class Mod(
    ISptLogger<Mod> _logger,
    DataService _data,
    SharedWeaponBuildsHelper _swbHelper,
    DatabaseService _db,
    ConfigServer _cfg,
    ImageRouter _imageRouter,
    ProfileHelper _profileHelper,
    RagfairPriceService _priceService,
    RagfairHelper _ragfairHelper,
    ICloner _cloner
) : IOnLoad
{
    public Task OnLoad()
    {
        var tbase = _data.GetBase();
        var config = _data.GetConfig();

        if (Enum.TryParse<CurrencyType>(_ragfairHelper.GetCurrencyTag(config.Currency), out var currency))
        {
            tbase.Currency = currency;
        }

        var traders = _db.GetTraders();
        traders.Add(
            tbase.Id,
            new()
            {
                Base = tbase,
                Assort = NewAssort(),
                QuestAssort = new()
                {
                    ["success"] = [],
                    ["started"] = [],
                    ["fail"] = [],
                },
                Dialogue = []
            });

        var locales = _db.GetLocales().Global.Values;
        foreach (var locale in locales)
        {
            locale.AddTransformer((Dictionary<string, string>? loc) =>
            {
                loc!.Add($"{tbase.Id} FullName", tbase.Name);
                loc.Add($"{tbase.Id} FirstName", tbase.Name);
                loc.Add($"{tbase.Id} Nickname", tbase.Nickname!);
                loc.Add($"{tbase.Id} Location", tbase.Location!);
                loc.Add($"{tbase.Id} Description", "EYE HAVE YOU");
                return loc;
            });
        }

        var traderConfig = _cfg.GetConfig<TraderConfig>();
        traderConfig.UpdateTime.Add(new()
        {
            Name = tbase.Id,
            TraderId = tbase.Id,
            Seconds = new() { Min = 1600, Max = 3600 }
        });

        _imageRouter.AddRoute(
            tbase.Avatar!.Replace(".jpg", ""),
            _data.GetImagePath()
        );

        new GetAssortPatch().Enable();
        new SaveBuildPatch().Enable();

        return Task.CompletedTask;
    }

    public async void SetAssort()
    {
        var traders = _db.GetTraders();

        var tbase = _data.GetBase();
        var config = _data.GetConfig();

        var assort = NewAssort();
        HashSet<MongoId> buildIdCache = [];

        // player presets
        foreach (var profile in _profileHelper.GetProfiles().Values)
        {
            if (profile.UserBuildData is null || profile.UserBuildData.WeaponBuilds is null)
            {
                continue;
            }

            foreach (var preset in profile.UserBuildData.WeaponBuilds)
            {
                AddPreset(assort, preset, GetPresetLoyaltyLevel(preset.Name) ?? 1, config, buildIdCache);
            }
        }

        // file presets
        foreach (var file in await _data.GetPresetFiles())
        {
            foreach (var preset in file.Builds)
            {
                AddPreset(assort, preset, GetPresetLoyaltyLevel(preset.Name) ?? file.LoyaltyLevel, config, buildIdCache);
            }
        }

        // Shared Weapon Builds compatibility
        foreach (var build in _swbHelper.GetSharedBuilds())
        {
            AddPreset(assort, build, GetPresetLoyaltyLevel(build.Name) ?? 1, config, buildIdCache);
        }

        traders[tbase.Id].Assort = assort;
    }

    private void AddPreset(
        TraderAssort assort,
        WeaponBuild preset,
        int loyaltyLevel,
        Config config,
        HashSet<MongoId> buildIdCache
    )
    {
        if (buildIdCache.Contains(preset.Id))
        {
            _logger.Warning($"Not adding duplicate preset {preset.Id}.");
            return;
        }

        var items = _cloner.Clone(preset.Items)!;

        var id = items[0].Id;
        var tpl = items[0].Template;

        try
        {
            assort.LoyalLevelItems.Add(id, loyaltyLevel);
        }
        catch
        {
            _logger.Warning($"Duplicate root item with ID {id}.");
            return;
        }

        items[0] = new()
        {
            Id = id,
            Template = tpl,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new()
            {
                UnlimitedCount = true,
                StackObjectsCount = 2000
            }
        };
        assort.Items.AddRange(items);

        var price = _priceService.GetDynamicOfferPriceForOffer(items, config.Currency, false) * config.PriceMultiplier;
        var barter = new BarterScheme()
        {
            Count = price,
            Template = config.Currency
        };
        assort.BarterScheme.Add(id, [[barter]]);

        buildIdCache.Add(preset.Id);
    }

    public static int? GetPresetLoyaltyLevel(string? name)
    {
        if ((name is null) || (name.Length < 2))
        {
            return null;
        }

        return name.Substring(name.Length - 2) switch
        {
            "-1" => 1,
            "-2" => 2,
            "-3" => 3,
            "-4" => 4,
            _ => null
        };
    }

    public static TraderAssort NewAssort()
    {
        return new()
        {
            Items = [],
            BarterScheme = [],
            LoyalLevelItems = [],
            NextResupply = 0
        };
    }
}

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1)]
public class AssortHydrator(Mod mod) : IOnLoad
{
    public Task OnLoad()
    {
        return Task.Run(mod.SetAssort);
    }
}
