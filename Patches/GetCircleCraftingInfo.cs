namespace CultistCircleImprovementsServer.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Services.Hideout;
using SPTarkov.Server.Core.Services.Locales;

public class PatchGetCircleCraftingInfo : AbstractPatch
{
    private static ServerLocalisationService _serverLocalisationService = null!;
    private static ISptLogger<CircleOfCultistService> _logger = null!;
    private static TimeUtil _timeUtil = null!;

    public PatchGetCircleCraftingInfo(ServerLocalisationService serverLocalisationService, ISptLogger<CircleOfCultistService> logger, TimeUtil timeUtil)
    {
        _serverLocalisationService = serverLocalisationService;
        _logger = logger;
        _timeUtil = timeUtil;
    }
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
            RewardDetails = null!,
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
        var matchingThreshold = thresholds.FirstOrDefault(craftThreshold =>
            craftThreshold.Min <= rewardAmountRoubles && craftThreshold.Max >= rewardAmountRoubles
        );

        // No matching threshold, make one
        if (matchingThreshold is null)
        {
            // None found, use a default
            _logger.Warning(_serverLocalisationService.GetText("cultistcircle-no_matching_threshhold_found", new { rewardAmountRoubles }));

            // Use first threshold value (cheapest) from parameter array, otherwise use 12 hours
            var firstThreshold = thresholds.FirstOrDefault();
            var craftTime = firstThreshold?.CraftTimeSeconds > 0 ? firstThreshold.CraftTimeSeconds : _timeUtil.GetHoursAsSeconds(12);

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