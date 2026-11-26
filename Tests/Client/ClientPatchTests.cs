using System.Reflection;
using System.Runtime.Serialization;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Insurance;
using EFT.UI.Matchmaker;
using EFT.UI.Ragfair;
using EFT.UI.Screens;
using HarmonyLib;
using Liquidwarp.FeatureTweaks.Client;
using Newtonsoft.Json;
using NUnit.Framework;
using InsuranceScreen = EFT.UI.Matchmaker.MatchmakerInsuranceScreen.InsuranceScreenController;

namespace Liquidwarp.FeatureTweaks.Tests;

[NonParallelizable]
public class ClientPatchTests
{
    private static void Configure(bool disableSelling = false, bool disableInsurance = false)
    {
        Plugin.EnableConfiguredPatches(url =>
        {
            Assert.That(url, Is.EqualTo("/featuretweaks/config"));
            return JsonConvert.SerializeObject(new { disableFleaSelling = disableSelling, disableInsurance, ammoStatsInName = true });
        });
    }

    [TearDown]
    public void RemoveModPatches()
    {
        var harmony = new Harmony("featuretweaks.tests");
        foreach (var target in Harmony.GetAllPatchedMethods().ToArray())
        {
            var info = Harmony.GetPatchInfo(target);
            foreach (var patch in info.Prefixes.Concat(info.Postfixes))
            {
                if (patch.PatchMethod.DeclaringType?.Assembly == typeof(Plugin).Assembly)
                    harmony.Unpatch(target, patch.PatchMethod);
            }
        }
    }

    [Test]
    public void StartupInstallsOnlyTheConfiguredPatches([Values] bool disableSelling, [Values] bool disableInsurance)
    {
        Configure(disableSelling, disableInsurance);
        var targets = new (MethodInfo Target, MethodInfo Hook, bool Enabled)[]
        {
            (typeof(RagFair).GetMethod(nameof(RagFair.GetMaxOffersCount), [typeof(float)])!,
                typeof(MaxOffersCountPatch).GetMethod(nameof(MaxOffersCountPatch.Postfix))!, disableSelling),
            (typeof(ItemContextInteractionsSwitcher).GetMethod(nameof(ItemContextInteractionsSwitcher.IsActive), [typeof(EItemInfoButton)])!,
                typeof(HideAddOfferPatch).GetMethod(nameof(HideAddOfferPatch.Postfix))!, disableSelling),
            (typeof(InsuranceCompany).GetMethod(nameof(InsuranceCompany.ItemTypeAvailableForInsurance), [typeof(Item)])!,
                typeof(ItemAvailableForInsurancePatch).GetMethod(nameof(ItemAvailableForInsurancePatch.Postfix))!, disableInsurance),
            (typeof(InsuranceCompany).GetMethod(nameof(InsuranceCompany.ItemTypeAvailableForInsurance), [typeof(InsuredItem)])!,
                typeof(InsuredItemAvailableForInsurancePatch).GetMethod(nameof(InsuredItemAvailableForInsurancePatch.Postfix))!, disableInsurance),
            (typeof(InsuranceScreen).GetMethod(nameof(InsuranceScreen.ShowScreen), [typeof(EScreenState)])!,
                typeof(SkipInsuranceScreenPatch).GetMethod(nameof(SkipInsuranceScreenPatch.Prefix))!, disableInsurance),
        };

        foreach (var (target, hook, enabled) in targets)
        {
            var info = Harmony.GetPatchInfo(target);
            var patches = hook.Name == "Prefix" ? info?.Prefixes : info?.Postfixes;
            Assert.That(patches?.Any(patch => patch.PatchMethod == hook) ?? false,
                Is.EqualTo(enabled), $"{hook.DeclaringType!.Name} on {target}");
        }
    }

    [TestCase("""{"disableFleaSelling":false}""")]
    [TestCase("""{"disableInsurance":false}""")]
    [TestCase("""{"disableFleaSelling":null,"disableInsurance":false}""")]
    [TestCase("""{"disableFleaSelling":false,"disableInsurance":null}""")]
    public void IncompleteServerResponseIsRejected(string json)
    {
        Assert.Throws<JsonSerializationException>(() => Plugin.EnableConfiguredPatches(_ => json));
    }

    [Test]
    public void FleaUsesTheGamesOfferLimitUnlessSellingIsDisabled([Values] bool disableSelling)
    {
        Configure(disableSelling: disableSelling);
        var flea = Uninitialized<RagFair>();
        flea._profile = Uninitialized<Profile>();
        typeof(Profile).GetField(nameof(Profile.Info))!.SetValue(flea._profile, Uninitialized<ProfileInfo>());
        var original = RagFair.Settings;
        try
        {
            RagFair.Settings = new RagfairSettings { maxActiveOfferCount = [new(0, 10, 7, 10)] };
            Assert.That(flea.GetMaxOffersCount(5), Is.EqualTo(disableSelling ? 0 : 7));
        }
        finally
        {
            RagFair.Settings = original;
        }
    }

    [Test]
    public void OnlyAddOfferIsHidden([Values] bool active)
    {
        foreach (EItemInfoButton button in Enum.GetValues(typeof(EItemInfoButton)))
        {
            var result = active;
            HideAddOfferPatch.Postfix(button, ref result);
            Assert.That(result, Is.EqualTo(active && button != EItemInfoButton.AddOffer), button.ToString());
        }
    }

    [Test]
    public void BothInsuranceOverloadsRespectTheSwitch([Values] bool disableInsurance, [Values] bool normallyEligible)
    {
        Configure(disableInsurance: disableInsurance);
        var template = Uninitialized<ItemTemplate>();
        template._id = "111111111111111111111111";
        template.IsAlwaysAvailableForInsurance = true;
        template.InsuranceDisabled = !normallyEligible;
        var item = new Item("111111111111111111111112", template);
        var insuredItem = Uninitialized<InsuredItem>();
        insuredItem.Item = item;
        var company = Uninitialized<InsuranceCompany>();

        var expected = normallyEligible && !disableInsurance;
        Assert.That(company.ItemTypeAvailableForInsurance(item), Is.EqualTo(expected));
        Assert.That(company.ItemTypeAvailableForInsurance(insuredItem), Is.EqualTo(expected));
    }

    [Test]
    public void InsuranceScreenAdvancesOnceAndSuppressesItsOriginalShow()
    {
        Configure(disableInsurance: true);
        var controller = Uninitialized<InsuranceScreen>();
        var advances = 0;
        controller.OnShowNextScreen += () => advances++;
        controller.ShowScreen(default);
        Assert.That(advances, Is.EqualTo(1));
    }

    [Test]
    public void OtherMatchmakerScreensRunTheirOriginalShow()
    {
        var controller = Uninitialized<MatchMakerAcceptScreen.MatchmakerAcceptScreenController>();
        Assert.That(SkipInsuranceScreenPatch.Prefix(controller), Is.True);
    }

    // These managed objects normally need a live game session in their constructors.
    private static T Uninitialized<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
}
