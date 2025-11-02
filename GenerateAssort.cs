using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using static SPTarkov.Server.Core.Controllers.BuildController;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Hephaestus
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class GenerateAssort(
            BuildController buildController,
            ICloner cloner,
            DatabaseService databaseService,
            ModHelper modHelper,
            ProfileHelper profileHelper,
            RagfairPriceService ragfairPriceService, JsonUtil jsonUtil)

    {


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

        public async void buildAssort()
        {
            try
            {

                // ensure mod assets can be resolved
                var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
                if (pathToMod is null)
                {
                    Console.WriteLine("getUserBuild: pathToMod is null");
                    return;
                }

                var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
                // config.json is an object, not a raw string — deserialize to JsonElement (or a typed config class)
                var config = JsonSerializer.Deserialize<HephaestusConfig>(modHelper.GetJsonDataFromFile<JsonElement>(pathToMod, "config.json"));

                if (traderBase is null)
                {
                    Console.WriteLine("getUserBuild: traderBase is null");
                    return;
                }

                var traders = databaseService.GetTables().Traders;
                var assort = NewAssort();
                var currency = Money.EUROS;
                if (config.currency is not null)
                {
                    currency = config.currency.ToString();
                }
                var discount = config.discount > 0 ? config.discount : 0;
                Console.WriteLine(discount);
                List<WeaponBuild> allBuilds = [];
                var presetFiles = System.IO.Directory.GetFiles(pathToMod + "/presets/", "*.json", new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive });
                foreach (var file in presetFiles)
                {
                    var presets = await jsonUtil.DeserializeFromFileAsync<List<WeaponBuild>>(file)!;
                    foreach (var preset in presets)
                    {
                        allBuilds.Add(preset);
                    }

                }
                foreach (var (sessionId, profile) in profileHelper.GetProfiles())
                {
                    if (profile.UserBuildData is null || profile.UserBuildData.WeaponBuilds is null)
                    {
                        continue;
                    }
                    foreach (var wb in profile.UserBuildData.WeaponBuilds)
                    {
                        allBuilds.Add(wb);
                    }
                }
                Console.WriteLine($"Hephaestus: Generated/Refreshed {allBuilds.Count()} builds");
                foreach (var wb in allBuilds)
                {
                    if (wb?.Items?[0]?.Id.IsValidMongoId() != true)
                    {
                        continue;
                    }
                    var preItems = wb.Items;
                    if (wb.Root is not null)
                    {
                        var pi = cloner.Clone(wb.Items)!;
                        var id = pi[0].Id;
                        var tpl = pi[0].Template;
                        pi[0] = new()
                        {
                            Id = id,
                            Template = tpl,
                            ParentId = "hideout",
                            SlotId = "hideout",
                            Upd = new()
                            {
                                UnlimitedCount = true,
                                StackObjectsCount = 999999
                            }
                        };
                        assort.Items.AddRange(pi);


                        var priceOfOfferItem = ragfairPriceService.GetDynamicOfferPriceForOffer(pi, config.currency, false);
                        if (discount > 0)
                        {
                            priceOfOfferItem = priceOfOfferItem - (priceOfOfferItem * (discount / 100));
                            
                        }

                        var barter = new BarterScheme()
                        {
                            Count = priceOfOfferItem,
                            Template = config.currency
                        };

                        assort.BarterScheme.Add(pi[0].Id, [[barter]]);
                        assort.LoyalLevelItems.Add(pi[0].Id, 1);
                    }
                }
                traders[traderBase.Id].Assort = assort;
            }
            catch (Exception ex)
            {
                // Keep this simple — replace with server logger if desired.
                Console.WriteLine($"getUserBuild exception: {ex}");
            }
        }
    }

    public class HephaestusConfig
    {
        public string currency { get; set; }
        public double discount { get; set; }

    }
}
