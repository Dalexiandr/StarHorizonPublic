using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Shared._Horizon.Cytology.Systems;

namespace Content.Client._Horizon.Cytology.Microscope.UI;

[UsedImplicitly]
public sealed partial class MicroscopeBoundUserInterface : BoundUserInterface
{

    [ViewVariables]
    private MicroscopeWindow? _window;


    public MicroscopeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MicroscopeWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (MicroscopeBoundUserInterfaceState) state;

        _window?.UpdateState(castState);
    }
}
