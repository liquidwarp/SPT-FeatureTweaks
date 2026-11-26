using System.Runtime.CompilerServices;
using HarmonyLib;
using Liquidwarp.FeatureTweaks.Server;
using NUnit.Framework;
using SPTarkov.Server.Core.Generators.Ragfair;

namespace Liquidwarp.FeatureTweaks.Tests;

[NonParallelizable]
public class FleaOfferTests : ConfigFixture
{
    [Test]
    public async Task OnlyDynamicOffersAreSuppressedWhenConfigured([Values] bool suppress)
    {
        var config = WithConfig(ModConfig.Default with { DisableFleaGeneratedOffers = suppress, DisableFleaSelling = !suppress });
        var generate = typeof(RagfairOfferGenerator).GetMethod(nameof(RagfairOfferGenerator.GenerateDynamicOffers))!;
        var prefix = typeof(GenerateDynamicOffersPatch).GetMethod(nameof(GenerateDynamicOffersPatch.Prefix))!;
        var harmony = new Harmony("featuretweaks.tests");
        try
        {
            await new Mod(config).OnLoadAsync();
            var modTargets = Harmony.GetAllPatchedMethods().Where(method =>
                Harmony.GetPatchInfo(method).Prefixes.Any(p => p.PatchMethod.DeclaringType?.Assembly == typeof(Mod).Assembly));
            Assert.That(modTargets, Is.EquivalentTo(suppress ? new[] { generate } : Array.Empty<System.Reflection.MethodInfo>()));
            if (suppress)
            {
                // Neither initial population nor restocking should reach the generator's dependencies.
                var generator = (RagfairOfferGenerator)RuntimeHelpers.GetUninitializedObject(typeof(RagfairOfferGenerator));
                generator.GenerateDynamicOffers();
                generator.GenerateDynamicOffers([[]]);
            }
        }
        finally
        {
            harmony.Unpatch(generate, prefix);
        }
    }
}
