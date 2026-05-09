using Content.Client.UserInterface.Fragments;
using Content.Shared._Horizon._Fractions.AnCo.Companions;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon._Fractions.AnCo.CartridgeLoader.Cartridges;

public sealed partial class CompanionUi : UIFragment
{
    private CompanionUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CompanionUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CompanionPDABoundUserInterfaceState appraisalUiState)
            return;

        _fragment?.UpdateState();
    }
}
