using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.Insurance;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Liquidwarp.FeatureTweaks.Client;

public class ItemAvailableForInsurancePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(InsuranceCompany), nameof(InsuranceCompany.ItemTypeAvailableForInsurance), [typeof(Item)]);

    [PatchPostfix]
    public static void Postfix(ref bool __result) => __result = false;
}

public class InsuredItemAvailableForInsurancePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(InsuranceCompany), nameof(InsuranceCompany.ItemTypeAvailableForInsurance), [typeof(InsuredItem)]);

    [PatchPostfix]
    public static void Postfix(ref bool __result) => __result = false;
}

public class SkipInsuranceScreenPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(MatchmakerInsuranceScreen.InsuranceScreenController), nameof(MatchmakerInsuranceScreen.InsuranceScreenController.ShowScreen), [typeof(EScreenState)]);

    [PatchPrefix]
    public static bool Prefix(object __instance)
    {
        if (__instance is not MatchmakerInsuranceScreen.InsuranceScreenController controller)
            return true;

        controller.ShowNextScreen();
        return false;
    }
}
