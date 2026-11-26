using Liquidwarp.FeatureTweaks.Server;
using NUnit.Framework;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace Liquidwarp.FeatureTweaks.Tests;

public abstract class ConfigFixture
{
    protected static readonly JsonUtil Json = new([new SptJsonConverterRegistrator()]);
    private DirectoryInfo _directory = null!;
    protected string ConfigPath => Path.Combine(_directory.FullName, "config.json");

    [SetUp]
    public void CreateModFolder() => _directory = Directory.CreateTempSubdirectory("FeatureTweaks-");

    [TearDown]
    public void RemoveModFolder() => _directory.Delete(recursive: true);

    protected ConfigService Load() => new(_directory.FullName, new FileUtil(), Json);

    protected ConfigService WithConfig(ModConfig config)
    {
        File.WriteAllText(ConfigPath, Json.Serialize(config));
        return Load();
    }
}
