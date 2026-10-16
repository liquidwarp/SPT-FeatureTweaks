using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace Liquidwarp.FeatureTweaks.Server;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class Mod(ConfigService configService) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct = default)
    {
        if (configService.Config.DisableFleaGeneratedOffers)
            new GenerateDynamicOffersPatch().Enable();

        return Task.CompletedTask;
    }
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class DatabaseTweaks(ConfigService configService, GlobalTable globalTable, TradersTable tradersTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct = default)
    {
        var config = configService.Config;

        if (config.FleaRequiredLevelOne)
            globalTable.Configuration.RagFair.MinUserLevel = 1;

        if (config.DisableTraderHealing)
        {
            foreach (var trader in tradersTable.Values)
                trader.Base.Medic = false;
        }

        return Task.CompletedTask;
    }
}
