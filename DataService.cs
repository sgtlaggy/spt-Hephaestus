using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;


namespace Drebin;

[Injectable(InjectionType = InjectionType.Singleton)]
public class DataService
{
    protected TraderBase? _base;
    protected static string _modDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    protected static string _presetsDir = System.IO.Path.Join(_modDir, "presets");
    protected static string _resourceDir = System.IO.Path.Join(_modDir, "resources");
    protected static string _baseFile = System.IO.Path.Join(_resourceDir, "base.json");
    protected static string _imageFile = System.IO.Path.Join(_resourceDir, "avatar.jpg");

    public DataService(JsonUtil jsonUtil)
    {
        _base = jsonUtil.DeserializeFromFile<TraderBase>(_baseFile);
    }

    public TraderBase GetBase() => _base ?? throw new Exception("Invalid trader base.");
    public string GetImagePath() => _imageFile;
    public string[] GetPresetFiles() => System.IO.Directory.GetFiles(
            _presetsDir,
            "*.json",
            new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }
    );
}
