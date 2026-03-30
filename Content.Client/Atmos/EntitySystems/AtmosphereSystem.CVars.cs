using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private EntitySystem.Subscriptions _subs; // Sunrise edit

    private void InitializeCVars()
    {
        _subs = new(this);
        _subs.CVar(_cfg, CCVars.AtmosHeatScale,
            v => HeatScale = MathF.Max(0.000001f, v),
            invokeImmediately: true);
    }

    public override void Shutdown()
    {
        _subs.Cleanup();
        base.Shutdown();
    }
}
