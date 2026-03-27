using Content.Shared.Atmos.Reactions;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    protected float[] GasFuelMask = new float[Atmospherics.AdjustedNumberOfGases];
    protected float[] GasOxidizerMask = new float[Atmospherics.AdjustedNumberOfGases];
    protected float[] GasSpecificHeats = new float[Atmospherics.AdjustedNumberOfGases];

    protected virtual void InitializeGases()
    {
        Array.Resize(ref GasFuelMask, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));
        Array.Resize(ref GasOxidizerMask, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));
        Array.Resize(ref GasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

        for (var i = 0; i < GasPrototypes.Length; i++)
        {
            GasFuelMask[i] = GasPrototypes[i].IsFuel ? 1f : 0f;
            GasOxidizerMask[i] = GasPrototypes[i].IsOxidizer ? 1f : 0f;
            GasSpecificHeats[i] = GasPrototypes[i].SpecificHeat;
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
