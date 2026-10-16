using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace Liquidwarp.FeatureTweaks.Client;

internal class ServerConfig
{
    [JsonProperty("disableFleaSelling", Required = Required.Always)]
    public bool DisableFleaSelling { get; set; }

    [JsonProperty("disableInsurance", Required = Required.Always)]
    public bool DisableInsurance { get; set; }
}

[BepInPlugin(MyModInfo.MOD_GUID, MyModInfo.MOD_NAME, MyModInfo.MOD_VERSION)]
[BepInDependency("com.SPT.core", MyModInfo.CLIENT_SPT_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource LogSource;

    private void Awake()
    {
        LogSource = Logger;

        var config = JsonConvert.DeserializeObject<ServerConfig>(RequestHandler.GetJson("/featuretweaks/config"));

        if (config.DisableFleaSelling)
        {
            new MaxOffersCountPatch().Enable();
            new HideAddOfferPatch().Enable();
        }

        if (config.DisableInsurance)
        {
            new ItemAvailableForInsurancePatch().Enable();
            new InsuredItemAvailableForInsurancePatch().Enable();
            new SkipInsuranceScreenPatch().Enable();
        }
    }
}
