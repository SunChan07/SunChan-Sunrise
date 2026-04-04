using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Sandbox;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.ThermalVision;
using Content.Shared._Sunrise.Sandbox;

namespace Content.Server.Sandbox

public partial class SandboxUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<SandboxSystem>
{
    // Sunrise-edit:
    partial void OnThermalVisionChanged()
    {
        if (_window == null)
            return;
        _window.ThermalVisionButton.Pressed = _sandbox.ThermalVisionActive;
    }
}
