using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Sunrise.Atmos;

[UsedImplicitly]
public sealed class HighBodyTempBlurOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private HighBodyTempBlurOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new HighBodyTempBlurOverlay();
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<HighBodyTempBlurOverlay>();
    }
}