using Content.Client.UserInterface.Systems.Sandbox;
using Content.Client.UserInterface.Systems.Sandbox.Windows;
using Content.Client.UserInterface.States;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Sandbox;

public sealed partial class SandboxUIController
{
    private partial void OnThermalVisionChanged()
    {
        if (_window == null)
            return;

        _window.ThermalVisionButton.Pressed = _sandbox.ThermalVisionActive;
    }
}
