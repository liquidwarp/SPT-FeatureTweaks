using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Utils;

namespace Liquidwarp.FeatureTweaks.Server;

public record ModConfig
{
    [JsonPropertyName("disableFleaGeneratedOffers")]
    public required bool DisableFleaGeneratedOffers { get; init; }

    [JsonPropertyName("disableFleaSelling")]
    public required bool DisableFleaSelling { get; init; }

    [JsonPropertyName("fleaRequiredLevelOne")]
    public required bool FleaRequiredLevelOne { get; init; }

    [JsonPropertyName("disableInsurance")]
    public required bool DisableInsurance { get; init; }

    [JsonPropertyName("disableTraderHealing")]
    public required bool DisableTraderHealing { get; init; }

    [JsonPropertyName("ammoStatsInName")]
    public required bool AmmoStatsInName { get; init; }

    public static readonly ModConfig Default = new()
    {
        DisableFleaGeneratedOffers = true,
        DisableFleaSelling = true,
        FleaRequiredLevelOne = true,
        DisableInsurance = true,
        DisableTraderHealing = true,
        AmmoStatsInName = true,
    };
}

[Injectable(InjectionType.Singleton)]
public class ConfigService
{
    public ModConfig Config { get; }

    public ConfigService(ModHelper modHelper, FileUtil fileUtil, JsonUtil jsonUtil)
        : this(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), fileUtil, jsonUtil) { }

    internal ConfigService(string modFolder, FileUtil fileUtil, JsonUtil jsonUtil)
    {
        var path = Path.Combine(modFolder, "config.json");

        if (!fileUtil.FileExists(path))
        {
            fileUtil.WriteFile(path, jsonUtil.Serialize(ModConfig.Default, indented: true)!);
            Config = ModConfig.Default;
            return;
        }

        Config = jsonUtil.Deserialize<ModConfig>(fileUtil.ReadFile(path))
            ?? throw new InvalidDataException($"Invalid config file: {path}");
    }
}
