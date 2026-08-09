namespace CultistCircleImprovementsServer;

using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class PatchPreloader(IEnumerable<IRuntimePatch> patches) : IOnLoad 
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }

        return Task.CompletedTask;
    }
}
