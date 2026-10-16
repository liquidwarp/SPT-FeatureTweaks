using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Ragfair;

namespace Liquidwarp.FeatureTweaks.Server;
public class GenerateDynamicOffersPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(RagfairOfferGenerator), nameof(RagfairOfferGenerator.GenerateDynamicOffers));

    [PatchPrefix]
    public static bool Prefix() => false;
}
