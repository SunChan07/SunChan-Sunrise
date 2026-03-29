using Content.Client.Atmos.EntitySystems;
using Content.Client.Graphics;
using Content.Shared.Atmos.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.Atmos.Overlays;

/// <summary>
/// Thermal gas heatmap overlay (disabled until temperature is added to GasOverlayData).
/// </summary>
public sealed class GasTileDangerousTemperatureOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = false;

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private GasTileOverlaySystem? _gasTileOverlay;
    private readonly SharedTransformSystem _xformSys;
    private EntityQuery<GasTileOverlayComponent> _overlayQuery;

    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private readonly List<Entity<MapGridComponent>> _grids = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GasTileDangerousTemperatureOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();
        _overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        // Нет данных температуры в GasOverlayData — отключаем до их появления.
        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());
        if (res.TemperatureTarget != null)
            args.WorldHandle.DrawTextureRect(res.TemperatureTarget.Texture, args.WorldBounds);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();
        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? TemperatureTarget;

        public void Dispose()
        {
            TemperatureTarget?.Dispose();
        }
    }
}