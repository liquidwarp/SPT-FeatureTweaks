using System.Reflection;
using Liquidwarp.FeatureTweaks.Server;
using NUnit.Framework;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Items;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

namespace Liquidwarp.FeatureTweaks.Tests;

[SetCulture("fr-FR")]
public class DatabaseTests : ConfigFixture
{
    private Dictionary<MongoId, TemplateItem> _items = null!;
    private GlobalLocaleDictionary _english = null!;
    private LocaleTable _locales = null!;
    private GlobalTable _globals = null!;
    private TradersTable _traders = null!;
    private readonly MongoId _customTrader = new();

    [SetUp]
    public void CreateDatabase()
    {
        _items = [];
        _english = [];
        _locales = new() { Global = new() { ["en"] = Locale(_english) }, Languages = [], Menu = [] };
        _globals = new()
        {
            Configuration = new()
            {
                RagFair = new() { MinUserLevel = 27 }, Mastering = [], ArenaEftTransferSettings = null!,
                RestrictionsInRaid = [], EventType = []
            },
            BotPresets = [], BotWeaponScatterings = [], ItemPresets = [], LocationInfection = []
        };
        _traders = new()
        {
            [Traders.THERAPIST] = TraderWithMedic(true),
            [_customTrader] = TraderWithMedic(true),
            [Traders.PRAPOR] = TraderWithMedic(false),
            [Traders.FENCE] = TraderWithMedic(null),
        };
    }

    [Test]
    public async Task DatabaseSwitchesActIndependently([Values] bool levelOne, [Values] bool disableHealing, [Values] bool showStats)
    {
        var round = AddItem("Round", BaseClasses.AMMO, new() { Damage = 42, PenetrationPower = 17 });
        await Apply(ModConfig.Default with
        {
            FleaRequiredLevelOne = levelOne, DisableTraderHealing = disableHealing, AmmoStatsInName = showStats
        });
        Assert.That(_globals.Configuration.RagFair.MinUserLevel, Is.EqualTo(levelOne ? 1 : 27));
        Assert.That(_traders[Traders.THERAPIST].Base.Medic, Is.EqualTo(!disableHealing));
        Assert.That(_traders[_customTrader].Base.Medic, Is.EqualTo(!disableHealing));
        Assert.That(_traders[Traders.PRAPOR].Base.Medic, Is.False);
        Assert.That(_traders[Traders.FENCE].Base.Medic, Is.EqualTo(disableHealing ? false : (bool?)null));
        Assert.That(NameOf(round), Is.EqualTo(showStats ? "Round <color=#808080>[42/17]</color>" : "Round"));
    }

    [TestCase(null, "[41.5/19]")]
    [TestCase(1, "[41.5/19]")]
    [TestCase(8, "[332/19]")]
    public async Task AmmoUsesTotalProjectileDamageAndSingleProjectilePenetration(int? projectiles, string expected)
    {
        var ammoFamily = AddItem("Ammo family", BaseClasses.AMMO, null);
        var round = AddItem("Round", ammoFamily, new() { Damage = 41.5, PenetrationPower = 19, ProjectileCount = projectiles });
        await Apply(ModConfig.Default);
        Assert.That(NameOf(round), Is.EqualTo($"Round <color=#808080>{expected}</color>"));
    }

    [Test]
    public async Task FragGrenadesShowFragmentStatsAndRadiusAndThrownGrenadesShowFuseTime()
    {
        var fragment = AddItem("Fragment", BaseClasses.AMMO, new() { Damage = 32.5, PenetrationPower = 12 });
        var props = new TemplateItemProperties
        {
            FragmentType = fragment, FragmentsCount = 6, MaxExplosionDistance = 7.5,
            ThrowType = ThrowWeapType.frag_grenade, ExplDelay = 9, explDelay = 2.5
        };
        var frag = AddItem("Frag", BaseClasses.THROW_WEAP, props);
        var smoke = AddItem("Smoke", BaseClasses.THROW_WEAP, props with { ThrowType = ThrowWeapType.smoke_grenade, explDelay = null });
        var launcher = AddItem("Launcher", BaseClasses.AMMO, props with { FuzeArmTimeSec = 0.1, Damage = 999, PenetrationPower = 99 });

        await Apply(ModConfig.Default);
        Assert.That(NameOf(frag), Is.EqualTo("Frag <color=#808080>[6×32.5/12 7.5m 2.5s]</color>"));
        Assert.That(NameOf(smoke), Is.EqualTo("Smoke <color=#808080>[9s]</color>"));
        Assert.That(NameOf(launcher), Is.EqualTo("Launcher <color=#808080>[6×32.5/12 7.5m]</color>"));
    }

    [TestCase(null)]
    [TestCase("bad-id")]
    [TestCase("111111111111111111111111")]
    public async Task UnavailableFragmentsLeaveTheOtherStatsIntact(string? fragment)
    {
        var props = new TemplateItemProperties
        {
            FragmentType = fragment, FragmentsCount = 5, ThrowType = ThrowWeapType.frag_grenade,
            MaxExplosionDistance = 7, ExplDelay = 4
        };
        var grenade = AddItem("Frag", BaseClasses.THROW_WEAP, props);
        var launcher = AddItem("Launcher", BaseClasses.AMMO, props with { FuzeArmTimeSec = 0.1 });
        var bullet = AddItem("Bullet", BaseClasses.AMMO, props with
        {
            AmmoType = "bullet", HasGrenaderComponent = true, Damage = 40, PenetrationPower = 5
        });
        await Apply(ModConfig.Default);
        Assert.That(NameOf(grenade), Is.EqualTo("Frag <color=#808080>[7m 4s]</color>"));
        Assert.That(NameOf(launcher), Is.EqualTo("Launcher <color=#808080>[7m]</color>"));
        Assert.That(NameOf(bullet), Is.EqualTo("Bullet <color=#808080>[40/5] [7m]</color>"));
    }

    [Test]
    public async Task ZeroFragmentsOmitsEvenAValidFragmentTemplate()
    {
        var fragment = AddItem("Fragment", BaseClasses.AMMO, new() { Damage = 30, PenetrationPower = 5 });
        var grenade = AddItem("Frag", BaseClasses.THROW_WEAP, new()
        {
            FragmentType = fragment, FragmentsCount = 0, ThrowType = ThrowWeapType.frag_grenade,
            MaxExplosionDistance = 7, ExplDelay = 4
        });
        await Apply(ModConfig.Default);
        Assert.That(NameOf(grenade), Is.EqualTo("Frag <color=#808080>[7m 4s]</color>"));
    }

    [TestCase("grenade", 0, null, 4, "[4×21/5 8m]")]
    [TestCase("bullet", 0.1, true, 4, "[4×21/5 8m]")]
    [TestCase("bullet", 0, null, 4, "[33/2]")]
    [TestCase("bullet", 0, false, 4, "[33/2]")]
    [TestCase("bullet", 0, true, 4, "[33/2] [4×21/5 8m]")]
    [TestCase("bullet", 0, true, 0, "[33/2]")]
    [TestCase("grenade", 0.1, true, 0, "[33/2]")]
    public async Task AmmoLabelsDistinguishImpactAndFragmentDamage(
        string ammoType, double armingTime, bool? hasGrenaderComponent, int fragments, string expected)
    {
        var fragment = AddItem("Fragment", BaseClasses.AMMO, new() { Damage = 21, PenetrationPower = 5 });
        var round = AddItem("Round", BaseClasses.AMMO, new()
        {
            AmmoType = ammoType, Damage = 33, PenetrationPower = 2, MaxExplosionDistance = 8,
            FragmentType = fragment, FragmentsCount = fragments, FuzeArmTimeSec = armingTime,
            HasGrenaderComponent = hasGrenaderComponent
        });
        await Apply(ModConfig.Default);
        Assert.That(NameOf(round), Is.EqualTo($"Round <color=#808080>{expected}</color>"));
    }

    [Test]
    public async Task EachLocaleKeepsItsOwnTextAndOnlyExistingEligibleFullNamesChange()
    {
        var ammo = AddItem("English name", BaseClasses.AMMO, new() { Damage = 20, PenetrationPower = 2 });
        var untranslated = AddItem("English only", BaseClasses.AMMO, new() { Damage = 1, PenetrationPower = 0 });
        var noProps = AddItem("No properties", BaseClasses.AMMO, null);
        var other = AddItem("Other item", BaseClasses.MEDS, new() { Damage = 10 });
        _english[$"{ammo} ShortName"] = "Short";
        _english[$"{ammo} Description"] = "Description";
        _locales.Global["fr"] = Locale(new() { [$"{ammo} Name"] = "Nom français" });

        // SPT may already have cached a locale before the mod registers its transformer.
        Assert.That(NameOf(ammo), Is.EqualTo("English name"));
        await Apply(ModConfig.Default);

        Assert.That(NameOf(ammo), Is.EqualTo("English name <color=#808080>[20/2]</color>"));
        Assert.That(NameOf(ammo, "fr"), Is.EqualTo("Nom français <color=#808080>[20/2]</color>"));
        Assert.That(_locales.Global["fr"].Value!.ContainsKey($"{untranslated} Name"), Is.False);
        Assert.That(NameOf(noProps), Is.EqualTo("No properties"));
        Assert.That(NameOf(other), Is.EqualTo("Other item"));
        var english = _locales.Global["en"];
        Assert.That(english.Value![$"{ammo} ShortName"], Is.EqualTo("Short"));
        Assert.That(english.Value![$"{ammo} Description"], Is.EqualTo("Description"));
        english.Clear();
        Assert.That(NameOf(ammo), Is.EqualTo("English name <color=#808080>[20/2]</color>"));
    }

    [Test]
    public async Task InstalledItemDatabaseAndLocalesAcceptTheTransformer()
    {
        var install = typeof(DatabaseTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "SPTPath").Value!;
        var database = Path.Combine(install, "SPT_Runtime", "SPT_Data", "database");
        _items = Json.DeserializeFromFile<Dictionary<MongoId, TemplateItem>>(Path.Combine(database, "templates", "items.json"))!;
        foreach (var language in new[] { "en", "fr" })
        {
            var path = Path.Combine(database, "locales", "global", language + ".json");
            _locales.Global[language] = new(() => Json.DeserializeFromFile<GlobalLocaleDictionary>(path)!, cacheValue: true);
        }

        const string buckshot = "560d5e524bdc2d25448b4571";
        const string gunpowder = "590c5a7286f7747884343aea";
        const string shg2 = "67446fdd752be02c220f27b3";
        var original = _locales.Global.ToDictionary(pair => pair.Key, pair => pair.Value.Value!);
        var pellet = _items[buckshot].Properties!;
        var stats = FormattableString.Invariant($"[{pellet.Damage * pellet.ProjectileCount}/{pellet.PenetrationPower}]");
        var rocket = _items[shg2].Properties!;
        var fragment = _items[rocket.FragmentType!].Properties!;
        var rocketStats = FormattableString.Invariant($"[{rocket.FragmentsCount}×{fragment.Damage}/{fragment.PenetrationPower} {rocket.MaxExplosionDistance}m]");
        Assert.That(pellet.ProjectileCount, Is.GreaterThan(1));
        await Apply(ModConfig.Default);
        foreach (var (language, before) in original)
        {
            Assert.That(NameOf(buckshot, language), Is.EqualTo($"{before[$"{buckshot} Name"]} <color=#808080>{stats}</color>"));
            Assert.That(NameOf(gunpowder, language), Is.EqualTo(before[$"{gunpowder} Name"]));
            Assert.That(NameOf(shg2, language), Is.EqualTo($"{before[$"{shg2} Name"]} <color=#808080>{rocketStats}</color>"));
        }
    }

    private static Trader TraderWithMedic(bool? medic) => new()
    {
        Base = new() { Medic = medic }, Assort = null!, Dialogue = [], QuestAssort = []
    };

    private static LazyLoad<GlobalLocaleDictionary> Locale(GlobalLocaleDictionary source) => new(() =>
    {
        var copy = new GlobalLocaleDictionary();
        foreach (var entry in source)
            copy.Add(entry.Key, entry.Value);
        return copy;
    }, cacheValue: true);

    private MongoId AddItem(string name, MongoId parent, TemplateItemProperties? props)
    {
        var item = new TemplateItem { Id = new MongoId(), Parent = parent, Type = "Item", Properties = props };
        _items.Add(item.Id, item);
        _english[$"{item.Id} Name"] = name;
        return item.Id;
    }

    private string NameOf(MongoId id, string language = "en") => _locales.Global[language].Value![$"{id} Name"];

    private Task Apply(ModConfig config)
    {
        var templates = new TemplateTable
        {
            Items = _items, Character = [], CustomisationStorage = [], Prestige = null!, Quests = [],
            RepeatableQuests = null!, Handbook = null!, Customization = [], Dialogue = null!, Profiles = [],
            Prices = [], DefaultEquipmentPresets = [], Achievements = [], CustomAchievements = [], LocationServices = null!
        };
        var baseClasses = new ItemBaseClassService(null!, templates, null!);
        baseClasses.HydrateItemBaseClassCache();
        var helper = new ItemHelper(null!, templates, null!, null!, baseClasses, null!, null!, null!, null!);
        return new DatabaseTweaks(WithConfig(config), _globals, _traders, templates, _locales, helper).OnLoadAsync();
    }
}
