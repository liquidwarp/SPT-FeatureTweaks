using System.Text.Json;
using System.Text.Json.Nodes;
using Liquidwarp.FeatureTweaks.Server;
using NUnit.Framework;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace Liquidwarp.FeatureTweaks.Tests;

public class ConfigTests : ConfigFixture
{
    private static readonly string[] Keys =
    [
        "disableFleaGeneratedOffers", "disableFleaSelling", "fleaRequiredLevelOne",
        "disableInsurance", "disableTraderHealing", "ammoStatsInName"
    ];

    [Test]
    public void FirstLoadCreatesAllSixEnabledDefaultsAndTheyReload()
    {
        var loaded = Load().Config;
        var saved = JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();
        Assert.That(saved.Select(entry => entry.Key), Is.EquivalentTo(Keys));
        foreach (var key in Keys)
            Assert.That(saved[key]!.GetValue<bool>(), Is.True, key);
        Assert.That(loaded, Is.EqualTo(ModConfig.Default));
        Assert.That(Load().Config, Is.EqualTo(loaded));
    }

    [TestCaseSource(nameof(Keys))]
    public async Task EachSwitchSurvivesDiskAndTheClientConfigRoute(string enabledKey)
    {
        var input = new JsonObject();
        foreach (var key in Keys)
            input[key] = key == enabledKey;
        var originalFile = input.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, originalFile);

        var config = Load();
        Assert.That(config.Config, Is.EqualTo(new ModConfig
        {
            DisableFleaGeneratedOffers = enabledKey == "disableFleaGeneratedOffers",
            DisableFleaSelling = enabledKey == "disableFleaSelling",
            FleaRequiredLevelOne = enabledKey == "fleaRequiredLevelOne",
            DisableInsurance = enabledKey == "disableInsurance",
            DisableTraderHealing = enabledKey == "disableTraderHealing",
            AmmoStatsInName = enabledKey == "ammoStatsInName"
        }));
        var router = new ConfigRouter(Json, new HttpResponseUtil(Json, null!), config);
        const string route = "/featuretweaks/config";
        Assert.That(router.CanHandle(route), Is.True);
        var result = await router.HandleStaticAsync(route, null, new MongoId(), "");
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse((string)result), input), Is.True, enabledKey);
        Assert.That(File.ReadAllText(ConfigPath), Is.EqualTo(originalFile));
    }

    [TestCaseSource(nameof(Keys))]
    public void OmittingAnyRequiredSwitchRejectsTheConfig(string missingKey)
    {
        var input = JsonNode.Parse(Json.Serialize(ModConfig.Default)!)!.AsObject();
        input.Remove(missingKey);
        File.WriteAllText(ConfigPath, input.ToJsonString());
        Assert.Throws<JsonException>(() => Load());
    }

    [Test]
    public void NonBooleanSwitchIsRejected()
    {
        var input = JsonNode.Parse(Json.Serialize(ModConfig.Default)!)!.AsObject();
        input["disableInsurance"] = "sometimes";
        File.WriteAllText(ConfigPath, input.ToJsonString());
        Assert.Throws<JsonException>(() => Load());
    }

    [TestCase("{", typeof(JsonException))]
    [TestCase("null", typeof(InvalidDataException))]
    [TestCase("", typeof(InvalidDataException))]
    public void InvalidConfigIsReportedAndPreserved(string contents, Type exception)
    {
        File.WriteAllText(ConfigPath, contents);
        Assert.Throws(exception, () => Load());
        Assert.That(File.ReadAllText(ConfigPath), Is.EqualTo(contents));
    }
}
