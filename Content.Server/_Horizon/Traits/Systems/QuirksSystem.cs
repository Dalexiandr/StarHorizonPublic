using System.Linq;
using Content.Server._Horizon.Medical.Limbs;
using Content.Server.Body.Systems;
using Content.Shared._Horizon.Traits;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Robust.Server.Containers;
using Robust.Server.GameObjects;

namespace Content.Server._Horizon.Traits;

public sealed class QuirksSystem : SharedQuirksSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly LimbSystem _limb = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TraitPendingBodyModificationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var root = _body.GetRootPartOrNull(uid);
            if (root is null)
                return;

            for (var i = comp.Parts.Count - 1; i >= 0; i--)
            {
                var item = comp.Parts[i];
                if (!ReplacePart(uid, (root.Value.Entity, root.Value.BodyPart), item.PartType, item.ParentPartType, item.Symmetry, item.ProtoId, item.SlotId))
                    break;
            }

            comp.Parts.Clear();

            for (var i = comp.Organs.Count - 1; i >= 0; i--)
            {
                var item = comp.Organs[i];
                if (!ReplaceOrgan(uid, (root.Value.Entity, root.Value.BodyPart), item.OrganSlot, item.OrganProto))
                    break;
            }

            comp.Organs.Clear();

            if (comp.Organs.Count <= 0 && comp.Parts.Count <= 0)
                RemCompDeferred(uid, comp);
        }
    }

    public bool ReplacePart(EntityUid uid, Entity<BodyPartComponent> root, BodyPartType removePartType, BodyPartType? parentPart, BodyPartSymmetry symmerty, string? protoId, string? slotId)
    {
        var parts = _body.GetBodyChildrenOfType(uid, removePartType).Where(x => x.Component.Symmetry == symmerty);
        bool success = false;

        if (parts.Count() <= 0)
        {
            if (protoId is null || slotId == null || parentPart == null)
                return true;

            var parents = _body.GetBodyChildrenOfType(uid, parentPart.Value).Where(x => x.Component.Symmetry == symmerty);
            if (parents.Count() <= 0)
                return false;

            var parent = parents.First();
            var newLimb = SpawnAtPosition(protoId, Transform(uid).Coordinates);
            if (TryComp<BodyPartComponent>(newLimb, out var limbComp) && limbComp.Symmetry == symmerty)
                _limb.TryAttachLimb(uid, slotId, (parent.Id, parent.Component), (newLimb, limbComp));

            return true;
        }

        foreach (var part in parts)
        {
            if (!_body.TryGetParentBodyPart(part.Id, out var parent, out var parentComp))
                continue;

            foreach (var child in _body.GetBodyPartChildren(part.Id, part.Component))
            {
                _limb.TryAmputate(uid, child.Id);
                QueueDel(child.Id);
            }

            _limb.TryAmputate(uid, part.Id);
            QueueDel(part.Id);

            // apparently chopping off limbs makes people bleed a lot. Who would have guessed?
            _bloodstream.TryModifyBleedAmount(uid, -10f);

            if (protoId is null || slotId == null)
                continue;

            var newLimb = SpawnAtPosition(protoId, Transform(uid).Coordinates);
            if (TryComp<BodyPartComponent>(newLimb, out var limbComp) && limbComp.Symmetry == symmerty)
                _limb.TryAttachLimb(uid, slotId, (parent.Value, parentComp), (newLimb, limbComp));

            success = true;
        }

        return success;
    }

    public bool ReplaceOrgan(EntityUid uid, Entity<BodyPartComponent> root, string organId, string protoId)
    {
        var parts = _body.GetBodyChildren(uid);
        bool success = false;

        foreach (var part in parts)
        {
            // Check if this part has the specified organ slot
            if (!_body.CanInsertOrgan(part.Id, organId))
                continue;

            // Get the container for this organ slot
            if (!_container.TryGetContainer(part.Id, SharedBodySystem.GetOrganContainerId(organId), out var container))
                continue;

            // Remove existing organ if present
            if (container.ContainedEntities.Count > 0)
            {
                var organ = container.ContainedEntities.First();
                _body.RemoveOrgan(organ);
                QueueDel(organ);
            }

            // Add new organ to the specific slot
            var newOrgan = Spawn(protoId, Transform(uid).Coordinates);
            if (_body.InsertOrgan(part.Id, newOrgan, organId))
                success = true;
            else
                QueueDel(newOrgan); // Clean up if insertion failed
        }

        return success;
    }
}
