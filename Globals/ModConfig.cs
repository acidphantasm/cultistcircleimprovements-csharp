using System.Reflection;
using _cultistCircleImprovements.Models;
using _cultistCircleImprovements.Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _cultistCircleImprovements.Globals;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class ModConfig : IOnLoad
{
    public ModConfig(
        ModHelper modHelper,
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        ISptLogger<ModConfig> logger,
        CultistCircleImprovements cultistCircleImprovements)
    {
        _modHelper = modHelper;
        _jsonUtil = jsonUtil;
        _fileUtil = fileUtil;
        _logger = logger;
        _cultistCircleImprovements = cultistCircleImprovements;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }
    
    private static ModHelper? _modHelper;
    private static JsonUtil? _jsonUtil;
    private static FileUtil? _fileUtil;
    private static ISptLogger<ModConfig>? _logger;
    private static CultistCircleImprovements? _cultistCircleImprovements;
    
    public static ServerConfig Config {get; private set;} = null!;
    public static ServerConfig OriginalConfig {get; private set;} = null!;

    public static List<DirectRewardSettings> CustomCrafts { get; private set; } = null!;
    public static List<DirectRewardSettings> ContentBackportCrafts { get; private set; } = null!;
    
    private static int _isActivelyProcessingFlag = 0;
    public static string _modPath = string.Empty;
    
    public async Task OnLoad()
    {
        Config = await _jsonUtil.DeserializeFromFileAsync<ServerConfig>(Path.Combine(_modPath , "config.json")) ?? throw new ArgumentNullException();
        OriginalConfig = DeepClone(Config);
        
        CustomCrafts = await _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(Path.Combine(_modPath, "Data", "Crafts.json")) ?? throw new ArgumentNullException();
        ContentBackportCrafts = await _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(Path.Combine(_modPath, "Data", "ContentBackportCrafts.json")) ?? throw new ArgumentNullException();
    }
    
    public static async Task<ConfigOperationResult> ReloadConfig()
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            var configPath = Path.Combine(_modPath, "config.json");
            var customCraftPath = Path.Combine(_modPath, "Data", "Crafts.json");
            var backportCraftPath = Path.Combine(_modPath, "Data", "ContentBackportCrafts.json");

            var configTask = _jsonUtil.DeserializeFromFileAsync<ServerConfig>(configPath) ?? throw new FileNotFoundException();
            await Task.WhenAll(configTask);

            Config = configTask.Result ?? throw new ArgumentNullException(nameof(Config));
            OriginalConfig = DeepClone(Config);

            var customCrafts = _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(customCraftPath) ?? throw new FileNotFoundException();
            await Task.WhenAll(customCrafts);
            CustomCrafts = customCrafts.Result ?? throw new ArgumentNullException(nameof(CustomCrafts));
            
            var backportCrafts = _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(backportCraftPath) ?? throw new FileNotFoundException();
            await Task.WhenAll(backportCrafts);
            ContentBackportCrafts = backportCrafts.Result ?? throw new ArgumentNullException(nameof(ContentBackportCrafts));
            
            await Task.Run(() => _cultistCircleImprovements.RunConfigLoad());
            return ConfigOperationResult.Success;
        }
        catch (Exception ex)
        {
            return ConfigOperationResult.Failure;
        }
        finally
        {
            Interlocked.Exchange(ref _isActivelyProcessingFlag, 0);
        }
    }
    
    public static async Task<ConfigOperationResult> SaveConfig()
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var configPath = Path.Combine(pathToMod, "config.json");
            var customCraftPath = Path.Combine(pathToMod, "Data", "Crafts.json");
            var backportCraftPath = Path.Combine(pathToMod, "Data", "ContentBackportCrafts.json");

            //Config
            var serializedConfigTask = Task.Run(() => _jsonUtil.Serialize(Config, true));
            await Task.WhenAll(serializedConfigTask);

            var writeConfigTask = _fileUtil.WriteFileAsync(configPath, serializedConfigTask.Result!);
            await Task.WhenAll(writeConfigTask);
            
            OriginalConfig = DeepClone(Config);

            //CustomCrafts
            var serializedCustomCraftsTask = Task.Run(() => _jsonUtil.Serialize(CustomCrafts, true));
            await Task.WhenAll(serializedCustomCraftsTask);

            var writeCustomCraftsTask = _fileUtil.WriteFileAsync(customCraftPath, serializedCustomCraftsTask.Result!);
            await Task.WhenAll(writeCustomCraftsTask);

            //BackportCrafts
            var serializedBackportCraftsTask = Task.Run(() => _jsonUtil.Serialize(ContentBackportCrafts, true));
            await Task.WhenAll(serializedBackportCraftsTask);

            var writeBackportCraftsTask = _fileUtil.WriteFileAsync(backportCraftPath, serializedBackportCraftsTask.Result!);
            await Task.WhenAll(writeBackportCraftsTask);

            await Task.Run(() => _cultistCircleImprovements.RunConfigLoad());
            return ConfigOperationResult.Success;
        }
        catch (Exception ex)
        {
            return ConfigOperationResult.Failure;
        }
        finally
        {
            Interlocked.Exchange(ref _isActivelyProcessingFlag, 0);
        }
    }
    
    private static T DeepClone<T>(T source)
    {
        var json = _jsonUtil.Serialize(source);
        return _jsonUtil.Deserialize<T>(json)!;
    }
}