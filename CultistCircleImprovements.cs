using _cultistCircleImprovements.Globals;
using _cultistCircleImprovements.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace _cultistCircleImprovements;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 420)]
public class CultistCircleImprovements(
    IReadOnlyList<SptMod> installedMods,
    ISptLogger<CultistCircleImprovements> logger,
    ConfigServer configServer) : IOnLoad
{
    
    private readonly HideoutConfig _hideoutConfig = configServer.GetConfig<HideoutConfig>();
    private List<DirectRewardSettings> _defaultDirectRewards = new List<DirectRewardSettings>();
        
    public Task OnLoad()
    {
        ModConfig.HasBackport = installedMods.Any(x => x.ModMetadata.ModGuid == "com.wtt.contentbackport");
        
        new PatchGetCircleCraftingInfo().Enable();
        
        StoreDefaults();
        RunConfigLoad();
        
        return Task.CompletedTask;
    }

    private void StoreDefaults()
    {
        _defaultDirectRewards = _hideoutConfig.CultistCircle.DirectRewards.ToList();
    }

    public void RunConfigLoad()
    {
        AdjustConfigValues();
        AdjustDirectRewardMappings();
    }
    
    private void AdjustConfigValues()
    {
        var cultistCircleConfig = _hideoutConfig.CultistCircle;

        cultistCircleConfig.MaxRewardItemCount = ModConfig.Config.MaxRewardItemCount;                                   // Max rewarded items
        cultistCircleConfig.RewardPriceMultiplierMinMax.Min = ModConfig.Config.RewardPriceMultiplierMinMax.Min;         // Min multiplier for the rewarded amount, also multiplied by hideout management skill - final value is the Rouble value sent to calculate rewards
        cultistCircleConfig.RewardPriceMultiplierMinMax.Max = ModConfig.Config.RewardPriceMultiplierMinMax.Max;         // Max multiplier for the rewarded amount, also multiplied by hideout management skill - final value is the Rouble value sent to calculate rewards
        cultistCircleConfig.BonusChanceMultiplier = ModConfig.Config.BonusChanceMultiplier;                             // Chance to get hideout/task rewards (ALWAYS true in vanilla SPT <= 4.0.13 - PR is in to fix)
        cultistCircleConfig.HideoutTaskRewardTimeSeconds = ModConfig.Config.HideoutTaskRewardTimeSeconds;               // How long a hideout/task reward takes to complete
        cultistCircleConfig.HighValueThresholdRub = ModConfig.Config.HighValueThresholdRub;                             // If sacrified rouble value is higher than this, reward high value
        cultistCircleConfig.HideoutCraftSacrificeThresholdRub = ModConfig.Config.HideoutCraftSacrificeThresholdRub;     // If sacrified rouble value is higher than this AND the BonusChanceMultiplier successfully rolls true, reward hideout/task items
        cultistCircleConfig.CraftTimeThresholds = ModConfig.Config.CraftTimeThresholds;                                 // Min/Max/Timers for value thresholds when failing to roll for hideout/task craft
        cultistCircleConfig.CraftTimeOverride = ModConfig.Config.CraftTimeOverride;                                     // Set to override seconds time, if -1 then not used
    }
    
    private void AdjustDirectRewardMappings()
    {
        var cultistCircleConfig = _hideoutConfig.CultistCircle;
        
        // Clone the default rewards back to the config
        cultistCircleConfig.DirectRewards = _defaultDirectRewards
            .Select(x => new DirectRewardSettings()
            {
                Reward = x.Reward.ToList(),
                RequiredItems = x.RequiredItems.ToList(),
                CraftTimeSeconds = x.CraftTimeSeconds,
                Repeatable = x.Repeatable
            })
            .ToList();
        
        if (ModConfig.VanillaCrafts.Count != 0)
        {
            AdjustVanillaCrafts();
        }
        
        if (ModConfig.CustomCrafts.Count != 0)
        {
            AddCustomCrafts();
        }
        
        if (ModConfig.ContentBackportCrafts.Count != 0 && ModConfig.HasBackport)
        {
            AddBackportCrafts();
        }
    }

    private void AdjustVanillaCrafts()
    {
        var cultistCircleConfig = _hideoutConfig.CultistCircle;

        var adjusted = 0;

        foreach (var modCraft in ModConfig.VanillaCrafts)
        {
            if (modCraft.Reward.Count == 0 || modCraft.RequiredItems.Count == 0)
                continue;

            var existing = cultistCircleConfig.DirectRewards.FirstOrDefault(x =>
                x.RequiredItems.OrderBy(i => i).SequenceEqual(modCraft.RequiredItems.OrderBy(i => i))
            );

            if (existing == null) 
                continue;
            
            existing.CraftTimeSeconds = modCraft.CraftTimeSeconds;
            existing.Repeatable = modCraft.Repeatable;

            adjusted++;
        }

        logger.Info($"[CCI] Adjusted {adjusted} Vanilla Cultist Circle crafts");
    }

    private void AddCustomCrafts()
    {
        var cultistCircleConfig = _hideoutConfig.CultistCircle;

        var toAdd = ModConfig.CustomCrafts
            .Where(drs =>
                drs.Reward.Count > 0 && drs.RequiredItems.Count > 0 &&
                !cultistCircleConfig.DirectRewards.Any(x =>
                    x.RequiredItems.OrderBy(i => i).SequenceEqual(drs.RequiredItems.OrderBy(i => i))
                )
            )
            .ToList();

        cultistCircleConfig.DirectRewards.AddRange(toAdd);

        logger.Info($"[CCI] Added {toAdd.Count} Cultist Circle crafts");
    }

    private void AddBackportCrafts()
    {
        var cultistCircleConfig = _hideoutConfig.CultistCircle;

        var toAdd = ModConfig.ContentBackportCrafts
            .Where(drs =>
                drs.Reward.Count > 0 && drs.RequiredItems.Count > 0 &&
                !cultistCircleConfig.DirectRewards.Any(x =>
                    x.RequiredItems.OrderBy(i => i).SequenceEqual(drs.RequiredItems.OrderBy(i => i))
                )
            )
            .ToList();

        cultistCircleConfig.DirectRewards.AddRange(toAdd);

        logger.Info($"[CCI] Added {toAdd.Count} Cultist Circle crafts utilizing WTT-Content Backport");
    }
}
