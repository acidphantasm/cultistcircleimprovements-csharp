namespace CultistCircleImprovementsServer.Globals;

using System.Reflection;
using Models.Enums;
using Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Helpers.Server;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Preload)]
public class ModConfig : IOnLoad
{
    public ModConfig(
        ModHelper modHelper,
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        CCIOnLoad cciOnLoad)
    {
        _modHelper = modHelper;
        _jsonUtil = jsonUtil;
        _fileUtil = fileUtil;
        _cciOnLoad = cciOnLoad;
        ModPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }
    
    private static ModHelper _modHelper = null!;
    private static JsonUtil _jsonUtil = null!;
    private static FileUtil _fileUtil = null!;
    private static CCIOnLoad _cciOnLoad = null!;
    
    public static ServerConfig Config {get; private set;} = null!;
    public static ServerConfig OriginalConfig {get; private set;} = null!;

    public static List<DirectRewardSettings> CustomCrafts { get; private set; } = null!;
    public static List<DirectRewardSettings> ContentBackportCrafts { get; private set; } = null!;
    public static List<DirectRewardSettings> VanillaCrafts { get; private set; } = null!;
    
    private static int _isActivelyProcessingFlag = 0;
    public static string ModPath = string.Empty;

    public static bool HasBackport = false;
    
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Config = await _jsonUtil.DeserializeFromFileAsync<ServerConfig>(Path.Combine(ModPath , "config.json"), cancellationToken) ?? throw new ArgumentNullException();
        OriginalConfig = DeepClone(Config);
        
        CustomCrafts = await _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(Path.Combine(ModPath, "Data", "Crafts.json"), cancellationToken) ?? throw new ArgumentNullException();
        ContentBackportCrafts = await _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(Path.Combine(ModPath, "Data", "ContentBackportCrafts.json"), cancellationToken) ?? throw new ArgumentNullException();
        VanillaCrafts = await _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(Path.Combine(ModPath, "Data", "VanillaCrafts.json"), cancellationToken) ?? throw new ArgumentNullException();
    }
    
    public static async Task<ConfigOperationResult> ReloadConfig(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            var configPath = Path.Combine(ModPath, "config.json");
            var customCraftPath = Path.Combine(ModPath, "Data", "Crafts.json");
            var backportCraftPath = Path.Combine(ModPath, "Data", "ContentBackportCrafts.json");
            var vanillaCraftPath = Path.Combine(ModPath, "Data", "VanillaCrafts.json");

            var configTask = _jsonUtil.DeserializeFromFileAsync<ServerConfig>(configPath, cancellationToken) ?? throw new FileNotFoundException();
            await Task.WhenAll(configTask);

            Config = configTask.Result ?? throw new ArgumentNullException(nameof(Config));
            OriginalConfig = DeepClone(Config);

            var customCrafts = _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(customCraftPath, cancellationToken) ?? throw new FileNotFoundException();
            await Task.WhenAll(customCrafts);
            CustomCrafts = customCrafts.Result ?? throw new ArgumentNullException(nameof(CustomCrafts));

            if (HasBackport)
            {
                var backportCrafts = _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(backportCraftPath, cancellationToken) ?? throw new FileNotFoundException();
                await Task.WhenAll(backportCrafts);
                ContentBackportCrafts = backportCrafts.Result ?? throw new ArgumentNullException(nameof(ContentBackportCrafts));
            }
            
            var vanillaCrafts = _jsonUtil.DeserializeFromFileAsync<List<DirectRewardSettings>>(vanillaCraftPath, cancellationToken) ?? throw new FileNotFoundException();
            await Task.WhenAll(vanillaCrafts);
            VanillaCrafts = vanillaCrafts.Result ?? throw new ArgumentNullException(nameof(VanillaCrafts));
            
            await Task.Run(() => _cciOnLoad.RunConfigLoad(), cancellationToken);
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
    
    public static async Task<ConfigOperationResult> SaveConfig(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var configPath = Path.Combine(pathToMod, "config.json");
            var customCraftPath = Path.Combine(pathToMod, "Data", "Crafts.json");
            var backportCraftPath = Path.Combine(pathToMod, "Data", "ContentBackportCrafts.json");
            var vanillaCraftPath = Path.Combine(pathToMod, "Data", "VanillaCrafts.json");

            // Config
            var serializedConfigTask = Task.Run(() => _jsonUtil.Serialize(Config, true));
            await Task.WhenAll(serializedConfigTask);

            var writeConfigTask = _fileUtil.WriteFileAsync(configPath, serializedConfigTask.Result!, cancellationToken);
            await Task.WhenAll(writeConfigTask);
            
            OriginalConfig = DeepClone(Config);

            // Custom Crafts
            var serializedCustomCraftsTask = Task.Run(() => _jsonUtil.Serialize(CustomCrafts, true));
            await Task.WhenAll(serializedCustomCraftsTask);

            var writeCustomCraftsTask = _fileUtil.WriteFileAsync(customCraftPath, serializedCustomCraftsTask.Result!, cancellationToken);
            await Task.WhenAll(writeCustomCraftsTask);

            if (HasBackport)
            {
                // Backport Crafts
                var serializedBackportCraftsTask = Task.Run(() => _jsonUtil.Serialize(ContentBackportCrafts, true));
                await Task.WhenAll(serializedBackportCraftsTask);

                var writeBackportCraftsTask = _fileUtil.WriteFileAsync(backportCraftPath, serializedBackportCraftsTask.Result!, cancellationToken);
                await Task.WhenAll(writeBackportCraftsTask);
            }

            // Vanilla Crafts
            var serializedVanillaCraftsTask = Task.Run(() => _jsonUtil.Serialize(VanillaCrafts, true));
            await Task.WhenAll(serializedVanillaCraftsTask);
            
            var vanillaCraftsTask = _fileUtil.WriteFileAsync(vanillaCraftPath, serializedVanillaCraftsTask.Result!, cancellationToken);
            await Task.WhenAll(vanillaCraftsTask);

            await Task.Run(() => _cciOnLoad.RunConfigLoad(), cancellationToken);
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