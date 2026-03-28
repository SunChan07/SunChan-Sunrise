using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        private GasReactionPrototype[] _gasReactions = [];

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions;

        protected override void InitializeGases()
        {
            base.InitializeGases();

            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// Returns the heat capacity of a gas mixture in J/K.
        /// </summary>
        public float GetHeatCapacity(GasMixture mixture, bool applyScaling = false)
        {
            // applyScaling больше не используется в расчёте — оставлен для совместимости сигнатур
            return GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
        }

        /// <summary>
        /// Returns the internal (thermal) energy of a gas mixture in Joules.
        /// </summary>
        public float GetThermalEnergy(GasMixture mixture)
        {
            return mixture.Temperature * GetHeatCapacity(mixture);
        }

        /// <summary>
        /// Returns the thermal energy given a pre-computed heat capacity.
        /// </summary>
        public float GetThermalEnergy(GasMixture mixture, float cachedHeatCapacity)
        {
            return mixture.Temperature * cachedHeatCapacity;
        }

        /// <summary>
        /// Merges a gas mixture into a receiver, conserving energy.
        /// </summary>
        public void Merge(GasMixture receiver, GasMixture giver)
        {
            if (receiver.Immutable)
                return;

            if (MathF.Abs(receiver.Temperature - giver.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider
                && giver.TotalMoles > 0f)
            {
                var combinedHeatCapacity = GetHeatCapacity(receiver) + GetHeatCapacity(giver);
                if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    receiver.Temperature = (GetThermalEnergy(receiver) + GetThermalEnergy(giver)) / combinedHeatCapacity;
            }

            NumericsHelpers.Add(receiver.Moles, giver.Moles);
        }

        /// <summary>
        /// Returns true if the mixture contains both fuel and oxidizer (can sustain ignition).
        /// </summary>
        public bool IsMixtureIgnitable(GasMixture mixture)
        {
            return IsMixtureFuel(mixture) && IsMixtureOxidizer(mixture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float GetHeatCapacityCalculation(float[] moles, bool space)
        {
            // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
            if (space && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            Span<float> tmp = stackalloc float[moles.Length];
            NumericsHelpers.Multiply(moles, GasSpecificHeats, tmp);
            // Adjust heat capacity by speedup, because this is primarily what
            // determines how quickly gases heat up/cool.
            return MathF.Max(NumericsHelpers.HorizontalAdd(tmp), Atmospherics.MinimumHeatCapacity);
        }

        public override bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        {
            Span<float> tmp = stackalloc float[Atmospherics.AdjustedNumberOfGases];
            NumericsHelpers.Multiply(mixture.Moles, GasFuelMask, tmp);
            return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
        }

        public override bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
        {
            Span<float> tmp = stackalloc float[Atmospherics.AdjustedNumberOfGases];
            NumericsHelpers.Multiply(mixture.Moles, GasOxidizerMask, tmp);
            return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
        }

        /// <summary>
        ///     Return speedup factor for pumped or flow-based devices that depend on MaxTransferRate.
        /// </summary>
        public float PumpSpeedup()
        {
            return Speedup;
        }

        /// <summary>
        ///     Add 'dQ' Joules of energy into 'mixture'.
        /// </summary>
        public void AddHeat(GasMixture mixture, float dQ)
        {
            var c = GetHeatCapacity(mixture);
            float dT = dQ / c;
            mixture.Temperature += dT;
        }

        /// <summary>
        ///     Divides a source gas mixture into several recipient mixtures, scaled by their relative volumes. Does not
        ///     modify the source gas mixture. Used for pipe network splitting. Note that the total destination volume
        ///     may be larger or smaller than the source mixture.
        /// </summary>
        public void DivideInto(GasMixture source, List<GasMixture> receivers)
        {
            var totalVolume = 0f;
            foreach (var receiver in receivers)
            {
                if (!receiver.Immutable)
                    totalVolume += receiver.Volume;
            }

            float? sourceHeatCapacity = null;
            var buffer = new float[Atmospherics.AdjustedNumberOfGases];

            foreach (var receiver in receivers)
            {
                if (receiver.Immutable)
                    continue;

                var fraction = receiver.Volume / totalVolume;

                if (MathF.Abs(receiver.Temperature - source.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
                {
                    if (receiver.TotalMoles == 0)
                        receiver.Temperature = source.Temperature;
                    else
                    {
                        sourceHeatCapacity ??= GetHeatCapacity(source);
                        var receiverHeatCapacity = GetHeatCapacity(receiver);
                        var combinedHeatCapacity = receiverHeatCapacity + sourceHeatCapacity.Value * fraction;
                        if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                            receiver.Temperature = (GetThermalEnergy(source, sourceHeatCapacity.Value * fraction) + GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
                    }
                }

                NumericsHelpers.Multiply(source.Moles, fraction, buffer);
                NumericsHelpers.Add(receiver.Moles, buffer);
            }
        }

        /// <summary>
        ///     Releases gas from this mixture to the output mixture.
        ///     If the output mixture is null, then this is being released into space.
        ///     It can't transfer air to a mixture with higher pressure.
        /// </summary>
        public bool ReleaseGasTo(GasMixture mixture, GasMixture? output, float targetPressure)
        {
            var outputStartingPressure = output?.Pressure ?? 0;
            var inputStartingPressure = mixture.Pressure;

            if (outputStartingPressure >= MathF.Min(targetPressure, inputStartingPressure - 10))
                return false;

            if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0)) return false;

            var pressureDelta = MathF.Min(targetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure) / 2f);
            var transferMoles = pressureDelta * (output?.Volume ?? Atmospherics.CellVolume) / (mixture.Temperature * Atmospherics.R);

            var removed = mixture.Remove(transferMoles);

            if (output != null)
                Merge(output, removed);

            return true;
        }

        /// <summary>
        ///     Pump gas from this mixture to the output mixture.
        ///     Amount depends on target pressure.
        /// </summary>
        public bool PumpGasTo(GasMixture mixture, GasMixture output, float targetPressure)
        {
            var outputStartingPressure = output.Pressure;
            var pressureDelta = targetPressure - outputStartingPressure;

            if (pressureDelta < 0.01)
                return false;

            if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0)) return false;

            var transferMoles = pressureDelta * output.Volume / (mixture.Temperature * Atmospherics.R);

            var removed = mixture.Remove(transferMoles);
            Merge(output, removed);
            return true;
        }

        /// <summary>
        ///     Scrubs specified gases from a gas mixture into a <see cref="destination"/> gas mixture.
        /// </summary>
        public void ScrubInto(GasMixture mixture, GasMixture destination, IReadOnlyCollection<Gas> filterGases)
        {
            var buffer = new GasMixture(mixture.Volume) { Temperature = mixture.Temperature };

            foreach (var gas in filterGases)
            {
                buffer.AdjustMoles(gas, mixture.GetMoles(gas));
                mixture.SetMoles(gas, 0f);
            }

            Merge(destination, buffer);
        }

        /// <summary>
        /// Calculates the dimensionless fraction of gas required to equalize pressure between two gas mixtures.
        /// </summary>
        public float FractionToEqualizePressure(GasMixture gasMixture1, GasMixture gasMixture2)
        {
            if (gasMixture1.Pressure < gasMixture2.Pressure)
            {
                (gasMixture1, gasMixture2) = (gasMixture2, gasMixture1);
            }

            var volumeRatio = gasMixture2.Volume / gasMixture1.Volume;
            var molesRatio = gasMixture2.TotalMoles / gasMixture1.TotalMoles;
            var temperatureRatio = gasMixture2.Temperature / gasMixture1.Temperature;
            var heatCapacityRatio = GetHeatCapacity(gasMixture2) / GetHeatCapacity(gasMixture1);

            var quadraticA = 1 + volumeRatio;
            var quadraticB = molesRatio - volumeRatio + heatCapacityRatio * (temperatureRatio + volumeRatio);
            var quadraticC = heatCapacityRatio * (molesRatio * temperatureRatio - volumeRatio);

            return (-quadraticB + MathF.Sqrt(quadraticB * quadraticB - 4 * quadraticA * quadraticC)) / (2 * quadraticA);
        }

        /// <summary>
        /// Determines the fraction of gas to transfer from mix1 to mix2 to reach targetPressure in mix2.
        /// </summary>
        [PublicAPI]
        public static float MolesToMaxPressure(GasMixture mix1, GasMixture mix2, float targetPressure)
        {
            if (mix1.TotalMoles <= 0f || mix1.Temperature <= 0f || targetPressure <= mix2.Pressure)
                return 0f;

            var result = (targetPressure - mix2.Pressure) * mix2.Volume / (mix1.Temperature * Atmospherics.R);
            return float.IsFinite(result) ? MathF.Max(0f, result) : 0f;
        }

        [PublicAPI]
        public static float FractionToMaxPressure(GasMixture mix1, GasMixture mix2, float targetPressure)
        {
            if (mix1.TotalMoles <= 0f || mix1.Temperature <= 0f || targetPressure <= mix2.Pressure)
                return 0f;

            var fraction = MolesToMaxPressure(mix1, mix2, targetPressure) / mix1.TotalMoles;
            return float.IsFinite(fraction) ? MathF.Max(0f, fraction) : 0f;
        }

        /// <summary>
        /// Determines the number of moles that need to be removed from a <see cref="GasMixture"/> to reach a target pressure threshold.
        /// </summary>
        public static float MolesToPressureThreshold(GasMixture gasMixture, float targetPressure)
        {
            return gasMixture.TotalMoles -
                   targetPressure * gasMixture.Volume / (Atmospherics.R * gasMixture.Temperature);
        }

        /// <summary>
        ///     Checks whether a gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        public bool IsMixtureProbablySafe(GasMixture? air)
        {
            if (air == null)
                return false;

            switch (air.Pressure)
            {
                case <= Atmospherics.WarningLowPressure:
                case >= Atmospherics.WarningHighPressure:
                    return false;
            }

            switch (air.Temperature)
            {
                case <= 260:
                case >= 360:
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Compares two TileAtmospheres to see if they are within acceptable ranges for group processing to be enabled.
        /// </summary>
        public GasCompareResult CompareExchange(TileAtmosphere sample, TileAtmosphere otherSample)
        {
            if (sample.AirArchived == null || otherSample.AirArchived == null)
                return GasCompareResult.NoExchange;

            return CompareExchange(sample.AirArchived, otherSample.AirArchived);
        }

        /// <summary>
        ///     Compares two gas mixtures to see if they are within acceptable ranges for group processing to be enabled.
        /// </summary>
        public GasCompareResult CompareExchange(GasMixture sample, GasMixture otherSample)
        {
            var moles = 0f;

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasMoles = sample.Moles[i];
                var delta = MathF.Abs(gasMoles - otherSample.Moles[i]);
                if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > gasMoles * Atmospherics.MinimumAirRatioToMove))
                    return (GasCompareResult)i;
                moles += gasMoles;
            }

            if (moles > Atmospherics.MinimumMolesDeltaToMove)
            {
                var tempDelta = MathF.Abs(sample.Temperature - otherSample.Temperature);
                if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                    return GasCompareResult.TemperatureExchange;
            }

            return GasCompareResult.NoExchange;
        }

        [PublicAPI]
        public override ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
        {
            var reaction = ReactionResult.NoReaction;
            var temperature = mixture.Temperature;
            var energy = GetThermalEnergy(mixture);

            foreach (var prototype in GasReactions)
            {
                if (energy < prototype.MinimumEnergyRequirement ||
                    temperature < prototype.MinimumTemperatureRequirement ||
                    temperature > prototype.MaximumTemperatureRequirement)
                    continue;

                var doReaction = true;
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    var req = prototype.MinimumRequirements[i];

                    if (!(mixture.GetMoles(i) < req))
                        continue;

                    doReaction = false;
                    break;
                }

                if (!doReaction)
                    continue;

                reaction = prototype.React(mixture, holder, this, HeatScale);
                if (reaction.HasFlag(ReactionResult.StopReactions))
                    break;
            }

            return reaction;
        }

        /// <summary>
        /// Adds an array of moles to a <see cref="GasMixture"/>.
        /// Guards against negative moles by clamping to zero.
        /// </summary>
        [PublicAPI]
        public static void AddMolsToMixture(GasMixture mixture, ReadOnlySpan<float> molsToAdd)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(mixture.Moles.Length, molsToAdd.Length, nameof(mixture.Moles.Length));

            NumericsHelpers.Add(mixture.Moles, molsToAdd);
            NumericsHelpers.Max(mixture.Moles, 0f);
        }

        public enum GasCompareResult
        {
            NoExchange = -2,
            TemperatureExchange = -1,
        }
    }
}
