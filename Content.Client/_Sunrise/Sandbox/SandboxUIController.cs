using Content.Client.Sandbox;

namespace Content.Client.UserInterface.Systems.Sandbox;

public partial class SandboxUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<SandboxSystem>
{
    partial void OnThermalVisionChanged()
    {
        if (_window == null)
            return;
        _window.ThermalVisionButton.Pressed = _sandbox.ThermalVisionActive;
    }
}
