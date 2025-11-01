using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;



using System.Reflection;
using Path = System.IO.Path;

namespace Hephaestus;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.alexkarpen.hephaestus";
    public override string Name { get; init; } = "Hephaestus";
    public override string Author { get; init; } = "alexkarpen";
    public override List<string>? Contributors { get; init; } = ["alexkarpen"];
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/alexkarpen/hephaestus/tree/csharp";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

// This line tells the class to load right after "PostDBModLoader" occurs
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class Hephaestus(
    ISptLogger<Hephaestus> logger,
    ModHelper modHelper,
    DatabaseService databaseService,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    ICloner cloner,
    FluentTraderAssortCreator fluentAssortCreator, // This is a custom class we add for this mod, we made it injectable so it can be accessed like other classes here
    AddCustomTraderHelper addCustomTraderHelper, // This is a custom class we add for this mod, we made it injectable so it can be accessed like other classes here
    GenerateAssort generateAssortHelper
    )
    : IOnLoad
{
    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();

    public Task OnLoad()
    {
        // A path to the mods files we use below
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        // A relative path to the trader icon to show
        var traderImagePath = Path.Combine(pathToMod, "db/Hephaestus.jpg");

        // The base json containing trader settings we will add to the server
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");

        // Create a helper class and use it to register our traders image/icon + set its stock refresh time
        imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), traderImagePath);
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase, 1600, 3600);

        // Add our trader to the config list, this lets it be seen by the flea market
        _ragfairConfig.Traders.TryAdd(traderBase.Id, true);

        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        fluentAssortCreator
            .CreateSingleAssortItem(ItemTpl.DRINK_PACK_OF_MILK)
            .AddStackCount(200)
            .AddBuyRestriction(10)
            .AddMoneyCost(Money.ROUBLES, 2000)
            .AddLoyaltyLevel(1)
            .Export(traderBase.Id);
        addCustomTraderHelper.AddTraderToLocales(traderBase, "Hephaestus", "You share the reseller license of your creations to Hephaestus and in return you get a hefty discount.");

        
        logger.Success("Added Hephaestus trader to server");
        new GetAssortPatchWeaponBuild().Enable();
        new GetAssortPatchWeaponDelete().Enable();
        generateAssortHelper.buildAssort();
        return Task.CompletedTask;
    }
}


