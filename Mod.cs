using Drebin.Compat;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Ragfair;
using SPTarkov.Server.Core.Utils.Cloners;


namespace Drebin;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class Mod(
    ISptLogger<Mod> _logger,
    TradersTable _traders,
    LocaleTable _locales,
    TraderConfig _traderConfig,
    DataService _data,
    ProfileDataPatch _profileDataPatch,
    ImageRouter _imageRouter,
    ProfileHelper _profileHelper,
    RagfairPriceService _priceService,
    RagfairHelper _ragfairHelper,
    ICloner _cloner
) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var tbase = _data.GetBase();
        var config = _data.GetConfig();

        if (Enum.TryParse<CurrencyType>(_ragfairHelper.GetCurrencyTag(config.Currency), out var currency))
        {
            tbase.Currency = currency;
        }

        _traders.Add(
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

        var locales = _locales.Global.Values;
        foreach (var locale in locales)
        {
            locale.AddTransformer((GlobalLocaleDictionary? loc) =>
            {
                loc!.Add($"{tbase.Id} FullName", tbase.Name);
                loc.Add($"{tbase.Id} FirstName", tbase.Name);
                loc.Add($"{tbase.Id} Nickname", tbase.Nickname!);
                loc.Add($"{tbase.Id} Location", tbase.Location!);
                loc.Add($"{tbase.Id} Description", "EYE HAVE YOU");
                return loc;
            });
        }

        _traderConfig.UpdateTime.Add(new()
        {
            Name = tbase.Id,
            TraderId = tbase.Id,
            Seconds = new() { Min = 1600, Max = 3600 }
        });

        _imageRouter.AddRoute(
            tbase.Avatar!.Replace(".jpg", ""),
            _data.GetImagePath()
        );
    }

    public async void SetAssort(MongoId sessionId)
    {
        var tbase = _data.GetBase();
        var config = _data.GetConfig();

        var assort = NewAssort();

        // file presets
        foreach (var file in await _data.GetPresetFiles())
        {
            foreach (var preset in file.Builds)
            {
                AddPreset(assort, preset, GetPresetLoyaltyLevel(preset.Name) ?? file.LoyaltyLevel, config);
            }
        }

        // player presets
        foreach (var profile in _profileHelper.GetProfiles().Values)
        {
            if (profile.UserBuildData is null || profile.UserBuildData.WeaponBuilds is null)
            {
                continue;
            }

            foreach (var preset in profile.UserBuildData.WeaponBuilds)
            {
                AddPreset(assort, preset, GetPresetLoyaltyLevel(preset.Name) ?? 1, config);
            }
        }

        // fika offline compatibility
        var otherPlayerBuilds = await _profileDataPatch.GetOtherPlayerBuilds(sessionId);
        foreach (var (profileId, builds) in otherPlayerBuilds)
        {
            // SaveServer.ProfileExists without another DI
            if (_profileHelper.IsPlayer(profileId))
            {
                continue;
            }

            foreach (var build in builds)
            {
                AddPreset(assort, build, GetPresetLoyaltyLevel(build.Name) ?? 1, config);
            }
        }

        _traders[tbase.Id].Assort = assort;
    }

    private void AddPreset(TraderAssort assort, WeaponBuild preset, int loyaltyLevel, Config config)
    {
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

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class AssortHydrator(Mod mod) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        mod.SetAssort(new());
    }
}
