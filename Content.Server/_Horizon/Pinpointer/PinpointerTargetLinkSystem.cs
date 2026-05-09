using Content.Shared._NF.Pinpointer;
using Content.Shared.Pinpointer;

namespace Content.Server._Horizon.Pinpointer;

public sealed class PinpointerTargetLinkSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinpointerComponent, PinpointerTargetsModifiedEvent>(OnTargetsModified);
    }

    /// <summary>
    /// Removes <see cref="PinpointerTargetComponent"/> backlinks when a pinpointer is removed.
    /// Called from <see cref="Content.Server.Pinpointer.PinpointerSystem"/> — only one subscription to
    /// <see cref="ComponentShutdown"/> is allowed per component type.
    /// </summary>
    public void CleanupLinksOnPinpointerShutdown(EntityUid uid, PinpointerComponent comp)
    {
        foreach (var t in comp.Targets)
        {
            RemoveLink(uid, t);
        }
    }

    private void OnTargetsModified(EntityUid uid, PinpointerComponent comp, ref PinpointerTargetsModifiedEvent args)
    {
        SyncLinks(uid, comp, args.PreviousTargets);
    }

    private void SyncLinks(EntityUid pinpointer, PinpointerComponent comp, HashSet<EntityUid> previousTargets)
    {
        var newSet = new HashSet<EntityUid>(comp.Targets);

        foreach (var old in previousTargets)
        {
            if (!newSet.Contains(old))
                RemoveLink(pinpointer, old);
        }

        foreach (var t in newSet)
        {
            if (!previousTargets.Contains(t))
                AddLink(pinpointer, t);
        }
    }

    private void AddLink(EntityUid pinpointer, EntityUid target)
    {
        if (!Exists(target))
            return;

        var comp = EnsureComp<PinpointerTargetComponent>(target);
        if (!comp.Entities.Contains(pinpointer))
            comp.Entities.Add(pinpointer);

        Dirty(target, comp);
    }

    private void RemoveLink(EntityUid pinpointer, EntityUid target)
    {
        if (!TryComp<PinpointerTargetComponent>(target, out var comp))
            return;

        comp.Entities.Remove(pinpointer);

        if (comp.Entities.Count == 0)
            RemComp<PinpointerTargetComponent>(target);
        else
            Dirty(target, comp);
    }
}
