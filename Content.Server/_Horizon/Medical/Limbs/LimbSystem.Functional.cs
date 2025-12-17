using System.Linq;
using System.Reflection;
using Content.Shared._Horizon.Medical.Limbs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;

namespace Content.Server._Horizon.Medical.Limbs;
public sealed partial class LimbSystem //: SharedLimbSystem
{
    private static readonly MethodInfo? SRaiseLocalEventRefMethod;
    static LimbSystem()
    {
        SRaiseLocalEventRefMethod = typeof(LimbSystem)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m is {Name: nameof(RaiseLocalEvent), IsGenericMethodDefinition: true})
            .FirstOrDefault(m =>
            {
                var pars = m.GetParameters();
                if (pars.Length != 3)
                    return false;

                if (pars[0].ParameterType != typeof(EntityUid))
                    return false;

                if (!pars[1].ParameterType.IsByRef)
                    return false;

                return pars[2].ParameterType == typeof(bool);
            });
    }

    private void AddLimb(Entity<HumanoidAppearanceComponent> body, string slot, Entity<BodyPartComponent> limb)
    {
        switch (limb.Comp.PartType)
        {
            case BodyPartType.Arm:
                if (limb.Comp.Children.Keys.Count == 0)
                    _body.TryCreatePartSlot(limb, limb.Comp.Symmetry == BodyPartSymmetry.Left ? "left hand" : "right hand", BodyPartType.Hand, out _);

                foreach (var slotId in limb.Comp.Children.Keys)
                {
                    var slotFullId = SharedBodySystem.GetPartSlotContainerId(slotId);
                    var child = _containers.GetContainer(limb, slotFullId);

                    foreach (var containedEnt in child.ContainedEntities)
                    {
                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                            && innerPart.PartType == BodyPartType.Hand)
                            _hands.AddHand(body, slotFullId, limb.Comp.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                    }
                }
                break;
            case BodyPartType.Hand:
                _hands.AddHand(body, SharedBodySystem.GetPartSlotContainerId(slot), limb.Comp.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                break;
            case BodyPartType.Leg:
                if (limb.Comp.Children.Keys.Count == 0)
                    _body.TryCreatePartSlot(limb, limb.Comp.Symmetry == BodyPartSymmetry.Left ? "left foot" : "right foot", BodyPartType.Foot, out _);
                break;
            case BodyPartType.Foot:
            case BodyPartType.Other:
            case BodyPartType.Torso:
            case BodyPartType.Head:
            case BodyPartType.Tail:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        foreach (var comp in EntityManager.GetComponents(limb))
        {
            if (comp is not IImplantable)
                continue;
            {
                var eventType = typeof(LimbAttachedEvent<>).MakeGenericType(comp.GetType());
                var limbAttachedEvent = Activator.CreateInstance(eventType, [limb.Owner, comp]);

                if (limbAttachedEvent != null)
                {
                    var closedMethod = SRaiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                    closedMethod.Invoke(this, [body.Owner, limbAttachedEvent, false]);
                }
            }

            foreach (var face in comp.GetType().GetInterfaces().Where(x => x.IsAssignableTo(typeof(IImplantable))))
            {
                var eventType = typeof(LimbAttachedEvent<>).MakeGenericType(face);
                var limbAttachedEvent = Activator.CreateInstance(eventType, [limb.Owner, comp]);

                if (limbAttachedEvent == null)
                    continue;

                var closedMethod = SRaiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                closedMethod.Invoke(this, [ body.Owner, limbAttachedEvent, false ]);
            }
        }
    }

    private void RemoveLimb(Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
        switch (limb.Comp3.PartType)
        {
            case BodyPartType.Arm:
                foreach (var limbSlotId in limb.Comp3.Children.Keys)
                {
                    var child = _containers.GetContainer(limb, SharedBodySystem.GetPartSlotContainerId(limbSlotId));

                    foreach (var containedEnt in child.ContainedEntities)
                    {
                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                            && innerPart.PartType == BodyPartType.Hand)
                            _hands.RemoveHand(body, SharedBodySystem.GetPartSlotContainerId(limbSlotId));
                    }
                }
                break;
            case BodyPartType.Hand:
                var parentSlot = _body.GetParentPartAndSlotOrNull(limb);
                if (parentSlot is not null)
                    _hands.RemoveHand(body, SharedBodySystem.GetPartSlotContainerId(parentSlot.Value.Slot));
                break;
            case BodyPartType.Leg:
            case BodyPartType.Foot:
            case BodyPartType.Other:
            case BodyPartType.Torso:
            case BodyPartType.Head:
            case BodyPartType.Tail:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        foreach (var comp in EntityManager.GetComponents(limb))
        {
            if (comp is not IImplantable)
                continue;
            {
                var eventType = typeof(LimbRemovedEvent<>).MakeGenericType(comp.GetType());
                var limbAttachedEvent = Activator.CreateInstance(eventType, limb.Owner, comp);
                if (limbAttachedEvent != null)
                {
                    var closedMethod = SRaiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                    closedMethod.Invoke(this, [body.Owner, limbAttachedEvent, false]);
                }
            }
            foreach (var face in comp.GetType().GetInterfaces().Where(x => x.IsAssignableTo(typeof(IImplantable))))
            {
                var eventType = typeof(LimbRemovedEvent<>).MakeGenericType(face);
                var limbAttachedEvent = Activator.CreateInstance(eventType, limb.Owner, comp);
                if (limbAttachedEvent == null)
                    continue;

                var closedMethod = SRaiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                closedMethod.Invoke(this, [body.Owner, limbAttachedEvent, false]);
            }
        }
    }
}
