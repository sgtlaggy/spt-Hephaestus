using System.Reflection;
using SPTarkov.Server.Core.Models.Spt.Mod;


namespace Drebin;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.sgtlaggy.drebin";
    public string Name { get; init; } = "Drebin";
    public string Author { get; init; } = "sgtlaggy";
    public SemanticVersioning.Version Version { get; init; } = new(Assembly.GetExecutingAssembly().GetName().Version!.ToString(3));
    public string? Url { get; init; } = "https://github.com/sgtlaggy/spt-hephaestus";
    public string License { get; init; } = "MIT";
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.5");
    public List<string>? Contributors { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public bool HasPrepatcher { get; init; }
}
