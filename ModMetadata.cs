using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace CultistCircleImprovementsServer;

public record ModMetadata : IModMetadata, IModBlazorMetadata
{
    public string ModGuid { get; init; } = "com.acidphantasm.cultistcircleimprovements";
    public string Name { get; init; } = "Cultist Circle Improvements";
    public string Author { get; init; } = "acidphantasm";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
    public string? WWWRootUrl { get; init; }
    public string? HomePage { get; init; } = "/cci";
    public string? HomePageDescription { get; init; } = "Configure CCI";
}