using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;


namespace Drebin;

public record Config
{
    [JsonPropertyName("currency")]
    public MongoId Currency { get; set; }

    [JsonPropertyName("priceMultiplier")]
    public double PriceMultiplier { get; set; }
}

[Injectable(InjectionType = InjectionType.Singleton)]
public class DataService
{
    protected TraderBase? _base;
    protected Config? _config;

    protected static string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    protected static string _configFile = System.IO.Path.Join(_modDir, "config.json");
    protected static string _presetsDir = System.IO.Path.Join(_modDir, "presets");
    protected static string _resourceDir = System.IO.Path.Join(_modDir, "resources");
    protected static string _baseFile = System.IO.Path.Join(_resourceDir, "base.json");
    protected static string _imageFile = System.IO.Path.Join(_resourceDir, "avatar.jpg");

    public DataService(JsonUtil jsonUtil)
    {
        _base = jsonUtil.DeserializeFromFile<TraderBase>(_baseFile);
        _config = jsonUtil.DeserializeFromFile<Config>(_configFile);
    }

    public Config GetConfig() => _config ?? throw new Exception("Invalid config.");
    public TraderBase GetBase() => _base ?? throw new Exception("Invalid trader base.");
    public string GetImagePath() => _imageFile;
    public string[] GetPresetFiles() => System.IO.Directory.GetFiles(
            _presetsDir,
            "*.json",
            new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }
    );
}
