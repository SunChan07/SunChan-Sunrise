using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly Subscriptions _subs = new();

    private void InitializeCVars()
    {
        _subs.CVar(_cfg, CCVars.AtmosHeatScale,
            v => HeatScale = MathF.Max(0.000001f, v),
            invokeImmediately: true);
    }

    public override void Shutdown()
    {
        _subs.Dispose();
        base.Shutdown();
    }
}
