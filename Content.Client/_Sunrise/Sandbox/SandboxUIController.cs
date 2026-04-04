namespace Content.Client.UserInterface.Systems.Sandbox;

public sealed partial class SandboxUIController
{
    // Sunrise-edit:
    partial void OnThermalVisionChanged()
    {
        if (_window == null)
            return;
        _window.ThermalVisionButton.Pressed = _sandbox.ThermalVisionActive;
    }
}
