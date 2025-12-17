using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Humanoid;
using Content.Shared._Horizon.Medical.Body;
using Content.Shared._Horizon.Medical.Surgery.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Medical.Limbs;
public sealed partial class LimbSystem //: SharedLimbSystem
{
    [Dependency] private readonly ContainerSystem _containers = null!;
    [Dependency] private readonly BodySystem _body = null!;
    [Dependency] private readonly HandsSystem _hands = null!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = null!;
    [Dependency] private readonly MetaDataSystem _metadata = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;

    private readonly EntProtoId _virtual = "PartVirtual";

    public bool AttachLimb(Entity<HumanoidAppearanceComponent> body, string slot, Entity<BodyPartComponent> part, Entity<BodyPartComponent> limb)
    {
        if (!_body.AttachPart(part, slot, limb, part.Comp, limb.Comp))
            return false;
        AddLimbVisual(body, limb);
        AddLimb(body, slot, limb);
        return true;
    }

    public bool AttachItem(EntityUid body, string slot, Entity<BodyPartComponent> part, Entity<MetaDataComponent> item)
    {
        var marker = EnsureComp<CustomLimbMarkerComponent>(item);

        var virtualItem = Spawn(_virtual);
        var virtualBodyPart = EnsureComp<BodyPartComponent>(virtualItem);
        var virtualCustomLimb = EnsureComp<CustomLimbComponent>(virtualItem);
        _metadata.SetEntityName(virtualItem, item.Comp.EntityName);

        marker.VirtualPart = virtualItem;
        virtualCustomLimb.Item = item;

        virtualBodyPart.PartType = BodyParts.GetBodyPart(slot);

        if (!_body.AttachPart(part, slot, virtualItem, part.Comp, virtualBodyPart))
        {
            QueueDel(virtualItem);
            return false;
        }
        AddItemLimb(body, slot, item);
        AddItemHand(body, item, SharedBodySystem.GetPartSlotContainerId(slot));
        return true;
    }

    public void Amputate(Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
        if (!_containers.TryGetContainingContainer((limb.Owner, limb.Comp1, limb.Comp2), out var container)
         || _body.GetParentPartAndSlotOrNull(limb.Owner) is not var (_, slotId)
         || !_containers.Remove(limb.Owner, container, destination: body.Comp1.Coordinates))
            return;

        if (TryComp<CustomLimbComponent>(limb, out var virtualLimb))
            AmputateItemLimb((body, body.Comp1, body.Comp3), limb, slotId, virtualLimb);
        else
        {
            RemoveLimbVisual(body, limb);
            RemoveLimb(body, limb);
        }
    }

    private void AddItemLimb(EntityUid body, string slot, Entity<MetaDataComponent> item)
    {
        var layer = VisualLayers.GetLayer(slot);
        var visualizer = EnsureComp<CustomLimbVisualizerComponent>(body);
        visualizer.Layers[layer] = GetNetEntity(item);
        Dirty(body, visualizer);
    }

    private void AmputateItemLimb(Entity<TransformComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb, string slotId, CustomLimbComponent virtualLimb)
    {
        RemoveItemLimb(body, virtualLimb.Item, SharedBodySystem.GetPartSlotContainerId(slotId));

        var visualizer = EnsureComp<CustomLimbVisualizerComponent>(body);

        var layer = VisualLayers.GetLayer(slotId);
        visualizer.Layers.Remove(layer);
        Dirty(body, visualizer);
        QueueDel(limb.Owner);
    }

    private void AddItemHand(EntityUid bodyId, EntityUid itemId, string handId)
    {
        if (!TryComp<HandsComponent>(bodyId, out var hands))
            return;

        if (!itemId.IsValid())
        {
            Log.Debug("no valid item");
            return;
        }

        _hands.AddHand(bodyId, handId, HandLocation.Middle, hands);
        _hands.DoPickup(bodyId, hands.Hands[handId], itemId, hands);
        EnsureComp<UnremoveableComponent>(itemId);
    }

    private void RemoveItemLimb(EntityUid bodyId, EntityUid itemId, string handId)
    {
        if (!bodyId.IsValid() || !itemId.IsValid())
            return;

        if (!TryComp<HandsComponent>(bodyId, out var hands)
            || !_hands.TryGetHand(bodyId, handId, out var hand, hands))
            return;

        RemComp<UnremoveableComponent>(itemId);
        _hands.DoDrop(itemId, hand);
        _hands.RemoveHand(bodyId, handId);
    }
}
