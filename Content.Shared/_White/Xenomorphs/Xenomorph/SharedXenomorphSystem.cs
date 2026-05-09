using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Log;

namespace Content.Shared._White.Xenomorphs.Xenomorph;

public abstract class SharedXenomorphSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.sharedxenomorph");
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string XenomorphItemTag = "XenomorphItem";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill.Debug("SharedXenomorphSystem initialized");
        SubscribeLocalEvent<XenomorphComponent, PickupAttemptEvent>(OnPickup);
    }

    private void OnPickup(EntityUid uid, XenomorphComponent component, PickupAttemptEvent args)
    {
        _sawmill.Debug($"OnPickup: uid={uid}, item={args.Item}");
        if (_tag.HasTag(args.Item, XenomorphItemTag))
        {
            _sawmill.Debug($"OnPickup: item has xenomorph tag, allowing pickup");
            return;
        }

        _sawmill.Debug($"OnPickup: item pickup cancelled");
        _popup.PopupClient(Loc.GetString("xenomorph-pickup-item-fail"), args.Item, uid);
        args.Cancel();
    }
}
