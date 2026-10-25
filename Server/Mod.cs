using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using static System.FormattableString;

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
public class DatabaseTweaks(
    ConfigService configService,
    GlobalTable globalTable,
    TradersTable tradersTable,
    TemplateTable templateTable,
    LocaleTable localeTable,
    ItemHelper itemHelper) : IOnLoad
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

        if (config.AmmoStatsInName)
            AddStatsToNames();

        return Task.CompletedTask;
    }

    private void AddStatsToNames()
    {
        var ammoStats = templateTable.Items.Values
            .Select(item => (item.Id, Stats: GetStats(item)))
            .Where(entry => entry.Stats is not null)
            .ToList();

        foreach (var locale in localeTable.Global.Values)
        {
            locale.AddTransformer(localeData =>
            {
                foreach (var (id, stats) in ammoStats)
                {
                    if (localeData!.TryGetValue($"{id} Name", out var name))
                        localeData[$"{id} Name"] = $"{name} <color=#808080>{stats}</color>";
                }

                return localeData;
            });
        }
    }

    private string? GetStats(TemplateItem item)
    {
        if (item.Properties is not { } props)
            return null;

        if (itemHelper.IsOfBaseclass(item.Id, BaseClasses.THROW_WEAP))
        {
            var fuse = Invariant($"{props.explDelay ?? props.ExplDelay}s");

            return props.ThrowType == ThrowWeapType.frag_grenade
                ? JoinStats(GetFragmentStats(props), Invariant($"{props.MaxExplosionDistance}m"), fuse)
                : JoinStats(fuse);
        }

        if (!itemHelper.IsOfBaseclass(item.Id, BaseClasses.AMMO))
            return null;

        // Launcher grenades: the direct hit stats are not what we care about
        if (props.FuzeArmTimeSec > 0 && props.FragmentsCount > 0)
            return JoinStats(GetFragmentStats(props), Invariant($"{props.MaxExplosionDistance}m"));

        return Invariant($"[{props.Damage * (props.ProjectileCount ?? 1)}/{props.PenetrationPower}]");
    }

    private string? GetFragmentStats(TemplateItemProperties props)
    {
        if (props.FragmentsCount is not > 0
            || !MongoId.IsValidMongoId(props.FragmentType!)
            || !templateTable.Items.TryGetValue(props.FragmentType!, out var fragment))
            return null;

        return Invariant($"{props.FragmentsCount}×{fragment.Properties?.Damage}/{fragment.Properties?.PenetrationPower}");
    }

    private static string JoinStats(params string?[] parts) =>
        $"[{string.Join(' ', parts.Where(part => part is not null))}]";
}
