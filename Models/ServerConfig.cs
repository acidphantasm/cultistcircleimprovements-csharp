using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace _cultistCircleImprovements.Models;

public record ServerConfig
{
    public int MaxRewardItemCount { get; set; }
    public required MinMax<double> RewardPriceMultiplierMinMax { get; init; }
    public double BonusChanceMultiplier { get; set; }
    public int HideoutTaskRewardTimeSeconds { get; set; }
    public int HighValueThresholdRub { get; set; }
    public int HideoutCraftSacrificeThresholdRub { get; set; }
    public int CraftTimeOverride { get; set; }
    public required List<CraftTimeThreshold>  CraftTimeThresholds { get; set; }
    public required ConfigAppSettings ConfigAppSettings { get; set; }
}

public record ConfigAppSettings
{
    public bool ShowUndo { get; set; }
    public bool ShowDefault { get; set; }
    public bool DisableAnimations { get; set; }
    public bool AllowUpdateChecks { get; set; }
}