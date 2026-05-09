using Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon._Fractions.AnCoDisposableFabricator;

[UsedImplicitly]
public sealed class AnCoDisposableFabricatorBoundUserInterface : BoundUserInterface
{
    private AnCoDisposableFabricatorMenu? _window;

    public AnCoDisposableFabricatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AnCoDisposableFabricatorMenu>();
        _window.OnApprove += SendApprove;
        _window.OnSetChange += SendChangeSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AnCoDisposableFabricatorBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new AnCoDisposableFabricatorChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new AnCoDisposableFabricatorApproveMessage());
    }
}
