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

    public static readonly ModConfig Default = new()
    {
        DisableFleaGeneratedOffers = true,
        DisableFleaSelling = true,
        FleaRequiredLevelOne = true,
        DisableInsurance = true,
        DisableTraderHealing = true,
    };
}

[Injectable(InjectionType.Singleton)]
public class ConfigService(ModHelper modHelper, FileUtil fileUtil, JsonUtil jsonUtil)
{
    public ModConfig Config { get; } = Load(modHelper, fileUtil, jsonUtil);

    private static ModConfig Load(ModHelper modHelper, FileUtil fileUtil, JsonUtil jsonUtil)
    {
        var path = Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config.json");

        if (!fileUtil.FileExists(path))
        {
            fileUtil.WriteFile(path, jsonUtil.Serialize(ModConfig.Default, indented: true)!);
            return ModConfig.Default;
        }

        return jsonUtil.Deserialize<ModConfig>(fileUtil.ReadFile(path))
            ?? throw new InvalidDataException($"Invalid config file: {path}");
    }
}
