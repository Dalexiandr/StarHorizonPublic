using Content.Shared._Horizon.OutpostCapture;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.OutpostCaptureUI;

[UsedImplicitly]
public sealed class OutpostConsoleBoundInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private OutpostCaptureWindow? _window;
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<OutpostCaptureWindow>();
        _window.CaptureButton.OnPressed += _ =>
        {
            SendMessage(new OutpostCaptureButtonPressed());
            _window.CaptureButton.Disabled = true;
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is ProgressBarUpdate update && _window != null)
        {
            _window.ProgressBar.Value = update.Value ?? 0;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState baseState)
    {
        base.UpdateState(baseState);
        if (_window == null || baseState is not OutpostUIState state)
            return;

        _window.ProgressBar.Value = state.Progress ?? 0;
        _window.CaptureButton.Disabled = state.Disabled;
        _window.CaptureButton.Text = state.ButtonState;
        _window.CaptureLabel.Text = state.LabelState;
    }
}
