using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Liquidwarp.FeatureTweaks.Client;

public class MaxOffersCountPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(RagFair), nameof(RagFair.GetMaxOffersCount));

    [PatchPostfix]
    public static void Postfix(ref int __result) => __result = 0;
}

public class HideAddOfferPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ItemContextInteractionsSwitcher), nameof(ItemContextInteractionsSwitcher.IsActive));

    [PatchPostfix]
    public static void Postfix(EItemInfoButton button, ref bool __result)
    {
        if (button == EItemInfoButton.AddOffer)
            __result = false;
    }
}
