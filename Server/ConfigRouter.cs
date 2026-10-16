using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace Liquidwarp.FeatureTweaks.Server;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class ConfigRouter(JsonUtil jsonUtil, HttpResponseUtil httpResponseUtil, ConfigService configService) : StaticRouter(
    jsonUtil,
    [
        new RouteAction(
            "/featuretweaks/config",
            (url, info, sessionId, output, cancellationToken) =>
                new ValueTask<object>(httpResponseUtil.NoBody(configService.Config)))
    ]);
