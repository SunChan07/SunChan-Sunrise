using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public static partial class CCVars
{
    public static readonly CVarDef<float> AtmosHeatScale =
        CVarDef.Create("atmos.heat_scale", 1.0f, CVar.SERVER | CVar.REPLICATED);
}
