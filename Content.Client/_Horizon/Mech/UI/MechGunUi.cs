using Content.Client.UserInterface.Fragments;
using Content.Shared._Horizon.Mech;
using Content.Shared._Horizon.Weapons.Ranged.Components;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Mech.UI;

public sealed partial class MechGunUi : UIFragment
{
    private MechGunUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner == null)
            return;

        var entManager = IoCManager.Resolve<IEntityManager>();

        _fragment = new MechGunUiFragment();

        _fragment.FragmentOwner = fragmentOwner;

        _fragment.ReloadAction += _ =>
        {
            userInterface.SendMessage(new MechGunReloadMessage(entManager.GetNetEntity(fragmentOwner.Value)));
            _fragment.StartTimer();
        };

        _fragment.SelectReagentAction += arg =>
        {
            userInterface.SendMessage(new SelectMechSyringeGunReagentMessage(entManager.GetNetEntity(fragmentOwner.Value), arg));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGunUiState gunState)
            return;

        _fragment?.UpdateContents(gunState);
    }
}
