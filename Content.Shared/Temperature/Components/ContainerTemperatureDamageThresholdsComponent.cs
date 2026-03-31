/// All Sunrise
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Overrides the temperature damage thresholds for entities inside this container.
/// Works in conjunction with <see cref="TemperatureDamageComponent"/>'s
/// ParentHeatDamageThreshold and ParentColdDamageThreshold fields.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ContainerTemperatureDamageThresholdsComponent : Component
{
    /// <summary>
    /// If set, overrides the HeatDamageThreshold for entities inside this container.
    /// </summary>
    [DataField]
    public float? HeatDamageThreshold;

    /// <summary>
    /// If set, overrides the ColdDamageThreshold for entities inside this container.
    /// </summary>
    [DataField]
    public float? ColdDamageThreshold;
}
