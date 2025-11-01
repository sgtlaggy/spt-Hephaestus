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
using SPTarkov.Server.Core.Utils.Cloners;
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using static SPTarkov.Server.Core.Controllers.BuildController;

namespace Hephaestus
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class GenerateAssort
    {
        private readonly BuildController _buildController;
        private readonly ICloner _cloner;
        private readonly DatabaseService _databaseService;
        private readonly ModHelper _modHelper;
        private readonly FluentTraderAssortCreator _fluentAssortCreator;
        private readonly RagfairPriceService _ragfairPriceService;
        private readonly ProfileHelper _profileHelper;

        public GenerateAssort(
            BuildController buildController,
            ICloner cloner,
            DatabaseService databaseService,
            ModHelper modHelper,
            ProfileHelper profileHelper,
            RagfairPriceService ragfairPriceService,
            FluentTraderAssortCreator fluentAssortCreator)
        {
            _buildController = buildController ?? throw new ArgumentNullException(nameof(buildController));
            _cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _modHelper = modHelper ?? throw new ArgumentNullException(nameof(modHelper));
            _ragfairPriceService = ragfairPriceService ?? throw new ArgumentNullException(nameof(ragfairPriceService));
            _fluentAssortCreator = fluentAssortCreator ?? throw new ArgumentNullException(nameof(fluentAssortCreator));
            _profileHelper = profileHelper ?? throw new ArgumentNullException(nameof(profileHelper));
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

        public void buildAssort()
        {
            try
            {
                
                // ensure mod assets can be resolved
                var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
                if (pathToMod is null)
                {
                    Console.WriteLine("getUserBuild: pathToMod is null");
                    return;
                }

                var traderBase = _modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
                Console.WriteLine(traderBase.Id);
                // config.json is an object, not a raw string — deserialize to JsonElement (or a typed config class)
                var config = JsonSerializer.Deserialize<HephaestusConfig>(_modHelper.GetJsonDataFromFile<JsonElement>(pathToMod, "config.json"));

                if (traderBase is null)
                {
                    Console.WriteLine("getUserBuild: traderBase is null");
                    return;
                }

               
                var traders = _databaseService.GetTables().Traders;
                var assort = NewAssort();
                var currency = Money.EUROS;
                if (config.currency is not null)
                {
                    currency = config.currency.ToString();
                }
                var discount = config.discount > 0 ? config.discount : 0;
                foreach (var (sessionId, profile) in _profileHelper.GetProfiles())
                {
                    if (profile.UserBuildData is null || profile.UserBuildData.WeaponBuilds is null)
                    {
                        continue;
                    }

                    foreach (var wb in profile.UserBuildData.WeaponBuilds)
                    {
                        if (wb?.Items?[0]?.Id.IsValidMongoId() != true)
                        {
                            Console.WriteLine("ekanan continue");
                            continue;
                        }
                        var preItems = wb.Items;
                        Console.WriteLine(wb.Name);
                        if (wb.Root is not null)
                        {
                            var pi = _cloner.Clone(wb.Items)!;
                            Console.WriteLine(pi.Count());
                            if(wb.Name == "111")
                            {
                                foreach (var item in pi)
                                {
                                    Console.WriteLine(item.Template);
                                }
                            }
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


                            var priceOfOfferItem = _ragfairPriceService.GetDynamicOfferPriceForOffer(pi, Money.ROUBLES, false);
                            if (discount > 0)
                            {
                                priceOfOfferItem = priceOfOfferItem - priceOfOfferItem * (1 - discount / 100);
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
