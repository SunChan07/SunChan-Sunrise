using Content.Shared.Temperature;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Atmos;

[UsedImplicitly]
public sealed class HighBodyTempBlurOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private const float StartK = 340f;
    private const float FullK = 2000f;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public HighBodyTempBlurOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _protoMan.Index<ShaderPrototype>("HeatBlur").InstanceUnique();
        ZIndex = 9000;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player is null)
            return false;

        if (!_entManager.TryGetComponent(player, out TemperatureComponent? tempComp))
            return false;

        return tempComp.CurrentTemperature >= StartK;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player is null)
            return;

        if (!_entManager.TryGetComponent(player, out TemperatureComponent? tempComp))
            return;

        var temp = tempComp.CurrentTemperature;
        var intensity = Math.Clamp((temp - StartK) / (FullK - StartK), 0f, 1f);

        // Вот сюда — применяем HeatBlur шейдер к экранной текстуре:
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", intensity); // float 0.0..1.0
        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }
}