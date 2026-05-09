using Content.Shared._Horizon.Medical.Limbs;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;

namespace Content.Server._Horizon.Medical.Limbs;
public sealed partial class CyberLimbSystem //: EntitySystem
{
    public void InitializeToggleable()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, LimbAttachedEvent<IWithAction>>(IWithActionAttached);
        SubscribeLocalEvent<BodyComponent, LimbRemovedEvent<IWithAction>>(IWithActionRemoved);
    }

    private void IWithActionRemoved(Entity<BodyComponent> ent, ref LimbRemovedEvent<IWithAction> args)
    {
        _actions.RemoveProvidedActions(ent.Owner, args.Limb);
    }

    private void IWithActionAttached(Entity<BodyComponent> ent, ref LimbAttachedEvent<IWithAction> args)
    {
        _actions.GrantContainedActions(_slEnt.Entity<ActionsComponent>(ent),
            _slEnt.Entity<ActionsContainerComponent>(args.Limb));
    }
}
