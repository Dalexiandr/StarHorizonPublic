using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared._Horizon.Mech;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Mech.UI;

public sealed partial class MechToolsUi : UIFragment
{
    private MechToolsUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner == null)
            return;

        _fragment = new MechToolsUiFragment();

        _fragment.SelectAction += arg =>
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            userInterface.SendMessage(new MechToolSetMessage(arg, entManager.GetNetEntity(fragmentOwner.Value)));
            _fragment.UpdateSelected(arg, entManager);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechToolsUiState cast)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();

        _fragment?.Startup(cast.Tools.Select(x => entMan.GetComponent<MetaDataComponent>(entMan.GetEntity(x)).EntityPrototype?.ID).ToList(), cast.SelectedTool, entMan);
        _fragment?.UpdateSelected(cast.SelectedTool, entMan);
    }
}
