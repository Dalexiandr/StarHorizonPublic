using Content.Shared._Horizon.ERTJuggernaut;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client._Horizon.ERTJuggernaut;

public sealed class JuggernautUIController : UIController
{
    [Dependency] private readonly IGameTiming _timing = default!;
    private JuggernautWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JuggernautSettingsActionEvent>((e) =>
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            Toggle();
        });
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<JuggernautWindow>();
    }

    private void Toggle()
    {
        EnsureWindow();

        if (_window?.IsOpen == true)
        {
            _window.Close();
        }
        else
        {
            _window?.Open();
        }
    }
}