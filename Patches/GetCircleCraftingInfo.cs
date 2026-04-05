using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace _cultistCircleImprovements.Patches;

public class PatchGetCircleCraftingInfo : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CircleOfCultistService),"GetCircleCraftingInfo");
    }

    [PatchPrefix]
    public static bool Prefix(ref CircleCraftDetails __result, double rewardAmountRoubles, CultistCircleSettings circleConfig, DirectRewardSettings? directRewardSettings = null)
    {
        var result = new CircleCraftDetails
        {
            Time = -1,
            RewardType = CircleRewardType.RANDOM,
            RewardAmountRoubles = (int)rewardAmountRoubles,
            RewardDetails = null,
        };

        // Direct reward edge case
        if (directRewardSettings is not null)
        {
            result.Time = directRewardSettings.CraftTimeSeconds;

            __result = result;
            return false;
        }

        var random = new Random();

        // Get a threshold where sacrificed amount is between thresholds min and max
        var matchingThreshold = GetMatchingThreshold(circleConfig.CraftTimeThresholds, rewardAmountRoubles);
        if (
            rewardAmountRoubles >= circleConfig.HideoutCraftSacrificeThresholdRub
            && random.NextDouble() <= circleConfig.BonusChanceMultiplier
        )
        {
            // Sacrifice amount is enough + passed 25% check to get hideout/task rewards
            result.Time = circleConfig.CraftTimeOverride != -1 ? circleConfig.CraftTimeOverride : circleConfig.HideoutTaskRewardTimeSeconds;
            result.RewardType = CircleRewardType.HIDEOUT_TASK;

            __result = result;
            return false;
        }

        // Edge case, check if override exists, Otherwise use matching threshold craft time
        result.Time = circleConfig.CraftTimeOverride != -1 ? circleConfig.CraftTimeOverride : matchingThreshold.CraftTimeSeconds;

        result.RewardDetails = matchingThreshold;

        __result = result;
        return false;
    }
    
    private static CraftTimeThreshold GetMatchingThreshold(List<CraftTimeThreshold> thresholds, double rewardAmountRoubles)
    {
        var localisationService = ServiceLocator.ServiceProvider.GetRequiredService<ServerLocalisationService>();
        var logger = ServiceLocator.ServiceProvider.GetRequiredService<ISptLogger<CircleOfCultistService>>();
        var timeUtil = ServiceLocator.ServiceProvider.GetRequiredService<TimeUtil>();
        
        var matchingThreshold = thresholds.FirstOrDefault(craftThreshold =>
            craftThreshold.Min <= rewardAmountRoubles && craftThreshold.Max >= rewardAmountRoubles
        );

        // No matching threshold, make one
        if (matchingThreshold is null)
        {
            // None found, use a default
            logger.Warning(
                localisationService.GetText("cultistcircle-no_matching_threshhold_found", new { rewardAmountRoubles = rewardAmountRoubles })
            );

            // Use first threshold value (cheapest) from parameter array, otherwise use 12 hours
            var firstThreshold = thresholds.FirstOrDefault();
            var craftTime = firstThreshold?.CraftTimeSeconds > 0 ? firstThreshold.CraftTimeSeconds : timeUtil.GetHoursAsSeconds(12);

            return new CraftTimeThreshold
            {
                Min = firstThreshold?.Min ?? 1,
                Max = firstThreshold?.Max ?? 34999,
                CraftTimeSeconds = craftTime,
            };
        }

        return matchingThreshold;
    }
}