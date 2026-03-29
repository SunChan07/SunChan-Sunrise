using System;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Robust.Shared.Maths;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    protected float[] GasFuelMask       = new float[Atmospherics.AdjustedNumberOfGases];
    protected float[] GasOxidizerMask   = new float[Atmospherics.AdjustedNumberOfGases];
    protected float[] GasSpecificHeats  = new float[Atmospherics.AdjustedNumberOfGases];

    protected virtual void InitializeGases()
    {
        var simdLen = MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4);
        Array.Resize(ref GasFuelMask,      simdLen);
        Array.Resize(ref GasOxidizerMask,  simdLen);
        Array.Resize(ref GasSpecificHeats, simdLen);

        for (var i = 0; i < GasPrototypes.Length; i++)
        {
            var proto = GasPrototypes[i];
            GasFuelMask[i]      = proto.IsFuel      ? 1f : 0f;
            GasOxidizerMask[i]  = proto.IsOxidizer  ? 1f : 0f;
            GasSpecificHeats[i] = proto.SpecificHeat;
        }
    }

    public virtual bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        => throw new NotImplementedException();

    public virtual bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        => throw new NotImplementedException();

    protected virtual float GetHeatCapacityCalculation(float[] moles, bool space)
        => throw new NotImplementedException();

    public virtual ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
        => ReactionResult.NoReaction;
}
