using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;


namespace Drebin;

using PresetFile = (int LoyaltyLevel, List<WeaponBuild> Builds);

public record Config
{
    [JsonPropertyName("currency")]
    public MongoId Currency { get; set; }

    [JsonPropertyName("priceMultiplier")]
    public double PriceMultiplier { get; set; }

    [JsonPropertyName("saveOtherProfileData")]
    public bool SaveOtherProfileData { get; set; }
}

[Injectable(InjectionType = InjectionType.Singleton)]
public class DataService
{
    protected ISptLogger<DataService> _logger;
    protected JsonUtil _jsonUtil;

    protected TraderBase? _base;
    protected Config? _config;

    protected static string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    protected static string _configFile = System.IO.Path.Join(_modDir, "config.json");
    protected static string _presetsDir = System.IO.Path.Join(_modDir, "presets");
    protected static string _resourceDir = System.IO.Path.Join(_modDir, "resources");
    protected static string _baseFile = System.IO.Path.Join(_resourceDir, "base.json");
    protected static string _imageFile = System.IO.Path.Join(_resourceDir, "avatar.jpg");

    public DataService(ISptLogger<DataService> logger, JsonUtil jsonUtil)
    {
        _logger = logger;
        _jsonUtil = jsonUtil;
        _base = jsonUtil.DeserializeFromFile<TraderBase>(_baseFile);
        _config = jsonUtil.DeserializeFromFile<Config>(_configFile);
    }

    public Config GetConfig() => _config ?? throw new Exception("Invalid config.");
    public TraderBase GetBase() => _base ?? throw new Exception("Invalid trader base.");
    public string GetImagePath() => _imageFile;
    public string[] GetPresetFilePaths() => System.IO.Directory.GetFiles(
            _presetsDir,
            "*.json",
            new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }
    );

    public async Task<List<PresetFile>> GetPresetFiles()
    {
        List<PresetFile> files = [];

        foreach (var file in GetPresetFilePaths())
        {
            var fileLevel = Mod.GetPresetLoyaltyLevel(System.IO.Path.GetFileNameWithoutExtension(file)) ?? 4;

            List<WeaponBuild>? presets;
            try
            {
                presets = await _jsonUtil.DeserializeFromFileAsync<List<WeaponBuild>>(file);
            }
            catch (Exception e)
            {
                _logger.Error($"Error reading {System.IO.Path.GetFileName(file)}:\n{e.ToString()}");
                continue;
            }

            files.Add(new(fileLevel, presets!));
        }

        return files;
    }
}
