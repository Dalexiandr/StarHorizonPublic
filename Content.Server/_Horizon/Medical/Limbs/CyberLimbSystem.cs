using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Shared._Horizon;
using Robust.Server.Audio;
using Robust.Server.Containers;

namespace Content.Server._Horizon.Medical.Limbs;
public sealed partial class CyberLimbSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = null!;
    [Dependency] private readonly StarlightEntitySystem _slEnt = null!;
    [Dependency] private readonly HandsSystem _hands = null!;
    //[Dependency] private readonly TransformSystem _xform = null!;
    [Dependency] private readonly ContainerSystem _container = null!;
    [Dependency] private readonly LimbSystem _limb = null!;
    [Dependency] private readonly AudioSystem _audio = null!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeLimbWithItems();
        InitializeToggleable();
    }
}
