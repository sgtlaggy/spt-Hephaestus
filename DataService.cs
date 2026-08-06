using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;


namespace Drebin;

public record PresetFile
{
    public int LoyaltyLevel { get; set; }
    public List<WeaponBuild> Builds { get; set; } = [];

    public PresetFile(int level, List<WeaponBuild> builds)
    {
        LoyaltyLevel = level;
        Builds = builds;
    }
}

public record Config
{
    [JsonPropertyName("currency")]
    public MongoId Currency { get; set; }

    [JsonPropertyName("priceMultiplier")]
    public double PriceMultiplier { get; set; }

    [JsonPropertyName("presetDirectories")]
    public IEnumerable<string> PresetDirectories { get; set; } = [];
}

[Injectable(InjectionType = InjectionType.Singleton)]
public class DataService
{
    protected static int DefaultFileLoyaltyLevel = 4;

    protected ISptLogger<DataService> _logger;
    protected FileUtil _fileUtil;
    protected JsonUtil _jsonUtil;

    protected TraderBase? _base;
    protected Config? _config;

    protected List<PresetFile>? _presetsCache;
    protected DateTime _lastRefresh;

    protected static string _modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    protected static string _configFile = Path.Join(_modDir, "config.json");
    protected static string _dbDir = Path.Join(_modDir, "db");
    protected static string _baseFile = Path.Join(_dbDir, "base.json");
    protected static string _imageFile = Path.Join(_dbDir, "avatar.jpg");

    public DataService(ISptLogger<DataService> logger, FileUtil fileUtil, JsonUtil jsonUtil)
    {
        _logger = logger;
        _fileUtil = fileUtil;
        _jsonUtil = jsonUtil;
        _base = jsonUtil.DeserializeFromFile<TraderBase>(_baseFile);
        _config = jsonUtil.DeserializeFromFile<Config>(_configFile);
    }

    public Config GetConfig() => _config ?? throw new Exception("Invalid config.");
    public TraderBase GetBase() => _base ?? throw new Exception("Invalid trader base.");
    public string GetImagePath() => _imageFile;
    public string[] GetPresetFilePaths(string dir) => Directory.GetFiles(
            dir,
            "*.json",
            new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }
    );

    public async Task<List<PresetFile>> GetPresetFiles()
    {
        var lastRefresh = _lastRefresh;
        var now = _lastRefresh = DateTime.Now;

        if ((_presetsCache is not null)
            && ((now - lastRefresh).TotalSeconds > 3))
        {
            return _presetsCache;
        }

        _presetsCache = [];

        foreach (var dir in GetConfig().PresetDirectories)
        {
            try
            {
                foreach (var file in GetPresetFilePaths(Path.Join(_modDir, dir)))
                {
                    var presets = await ReadPresetFile(file);
                    if (presets is not null)
                    {
                        _presetsCache.Add(presets);
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                _logger.Warning($"Preset directory {dir} not found.");
            }
        }

        return _presetsCache;
    }

    protected async Task<PresetFile?> ReadPresetFile(string file)
    {
        int? fileLevel = Mod.GetPresetLoyaltyLevel(Path.GetFileNameWithoutExtension(file)) ?? null;
        List<WeaponBuild>? presets;
        try
        {
            var content = await _fileUtil.ReadFileAsync(file);
            var firstChar = content.SkipWhile((ch) => Char.IsWhiteSpace(ch)).FirstOrDefault();
            if (firstChar == '[')
            {
                presets = _jsonUtil.Deserialize<List<WeaponBuild>>(content)!;
                fileLevel = fileLevel ?? DefaultFileLoyaltyLevel;
            }
            else if (firstChar == '{')
            {
                var preset = _jsonUtil.Deserialize<WeaponBuild>(content)!;
                presets = new([preset]);
                fileLevel = fileLevel ?? Mod.GetPresetLoyaltyLevel(preset.Name) ?? DefaultFileLoyaltyLevel;
            }
            else
            {
                _logger.Warning($"Error reading {Path.GetFileName(file)}: Does not contain weapon build.");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.Error($"Error reading {Path.GetFileName(file)}:\n{e.ToString()}");
            return null;
        }

        return new(fileLevel.Value, presets);
    }
}
