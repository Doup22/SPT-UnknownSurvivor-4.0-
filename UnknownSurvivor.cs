using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using WTTCommonLib;
using Path = System.IO.Path;


namespace UnknownSurvivor;

// This record holds the various properties for your mod
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "4d8499d3-ac24-488e-b021-32317c60f23f";
    public override string Name { get; init; } = "Unknown Survivor";
    public override string Author { get; init; } = "Dsnyder";
    public override List<string>? Contributors { get; set; } = ["Dsnyder"];
    public override SemanticVersioning.Version Version { get; } = new("1.0.0");
    public override SemanticVersioning.Version SptVersion { get; } = new("4.0.0");
    public override List<string>? LoadBefore { get; set; } = [""];
    public override List<string>? LoadAfter { get; set; } = [""];
    public override List<string>? Incompatibilities { get; set; } = [""];
    public override Dictionary<string, SemanticVersioning.Version>? ModDependencies { get; set; }
    public override string? Url { get; set; } = "https://github.com/Doup22/SPT-UnknownSurvivor";
    public override bool? IsBundleMod { get; set; } = true;
    public override string? License { get; init; } = "MIT";
}

/// <summary>
/// Feel free to use this as a base for your mod
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class UnknownSurvivorClass(
    ModHelper modHelper,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    AddCustomTraderHelper addCustomTraderHelper, // This is a custom class we add for this mod, we made it injectable so it can be accessed like other classes here
    WTTCommonLibPostDb  commonLib
)
    : IOnLoad
{
    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();


    public Task OnLoad()
    {
        string configDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "db", "items");
        commonLib.ItemService.CreateCustomItems(configDirectory);

        // A path to the mods files we use below
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        // A relative path to the trader icon to show
        var traderImagePath = Path.Combine(pathToMod, "res/unknownsurvivor.jpg");

        // The base json containing trader settings we will add to the server
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");

        // Create a helper class and use it to register our traders image/icon + set its stock refresh time
        imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), traderImagePath);
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));

        // Add our trader to the config file, this lets it be seen by the flea market
        _ragfairConfig.Traders.TryAdd(traderBase.Id, true);

        // Add our trader (with no items yet) to the server database
        // An 'assort' is the term used to describe the offers a trader sells, it has 3 parts to an assort
        // 1: The item
        // 2: The barter scheme, cost of the item (money or barter)
        // 3: The Loyalty level, what rep level is required to buy the item from trader
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase);

        // Add localisation text for our trader to the database so it shows to people playing in different languages
        addCustomTraderHelper.AddTraderToLocales(traderBase, "Survivor", "Not much is known about this trader.");

        // Get the assort data from JSON
        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "db/assort.json");

        // Save the data we loaded above into the trader we've made
        addCustomTraderHelper.OverwriteTraderAssort(traderBase.Id, assort);

        // Send back a success to the server to say our trader is good to go
        return Task.CompletedTask;
    }
}
