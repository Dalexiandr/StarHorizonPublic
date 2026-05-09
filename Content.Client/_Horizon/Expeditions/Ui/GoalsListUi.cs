using Content.Client.UserInterface.Fragments;
using Content.Shared._Horizon.Expeditions;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Expeditions.Ui;

/// <summary>
/// UI фрагмент, хранящий в себе цели
/// </summary>
public sealed partial class GoalsListUi : UIFragment
{
    private GoalsListUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new();
        _fragment.RemovePressed += args => userInterface.SendMessage(new CartridgeUiMessage(new GoalsListRemoveMessage(args)));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GoalsListCartridgeUiState cast)
            return;

        _fragment?.Populate(cast.Goals, IoCManager.Resolve<IEntityManager>());
    }
}
