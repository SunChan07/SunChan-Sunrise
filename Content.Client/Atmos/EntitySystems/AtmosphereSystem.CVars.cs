using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        // AtmosHeatScale is SERVERONLY but IS replicated to clients.
        // invokeImmediately: true ensures HeatScale is set before any test/game logic runs.
        Subs.CVar(_cfg, CCVars.AtmosHeatScale, value => HeatScale = value, true);
    }
}
