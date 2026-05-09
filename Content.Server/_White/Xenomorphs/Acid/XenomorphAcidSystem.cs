using Content.Shared._White.Xenomorphs.Acid;
using Content.Shared._White.Xenomorphs.Acid.Components;
using Content.Server.Actions;
using Content.Shared.Damage;
using Robust.Shared.Log;

namespace Content.Server._White.Xenomorphs.Acid;

public sealed class XenomorphAcidSystem : SharedXenomorphAcidSystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.xenomorphacid");
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill.Debug("XenomorphAcidSystem initialized");
        SubscribeLocalEvent<XenomorphAcidComponent, MapInitEvent>(OnXenomorphAcidMapInit);
        SubscribeLocalEvent<XenomorphAcidComponent, ComponentShutdown>(OnXenomorphAcidShutdown);
    }

    private void OnXenomorphAcidMapInit(EntityUid uid, XenomorphAcidComponent component, MapInitEvent args)
    {
        _sawmill.Debug($"OnXenomorphAcidMapInit: uid={uid}");
        _actions.AddAction(uid, ref component.AcidAction, component.AcidActionId);
    }

    private void OnXenomorphAcidShutdown(EntityUid uid, XenomorphAcidComponent component, ComponentShutdown args)
    {
        _sawmill.Debug($"OnXenomorphAcidShutdown: uid={uid}");
        _actions.RemoveAction(uid, component.AcidAction);
    }

    public override void Update(float frameTime)
    {
        var time = Timing.CurTime;

        var acidCorrodingQuery = EntityQueryEnumerator<AcidCorrodingComponent>();
        var count = 0;
        while (acidCorrodingQuery.MoveNext(out var uid, out var acidCorrodingComponent))
        {
            count++;
            if (time > acidCorrodingComponent.NextDamageAt)
            {
                _damageable.TryChangeDamage(uid, acidCorrodingComponent.DamagePerSecond);
                acidCorrodingComponent.NextDamageAt = time + TimeSpan.FromSeconds(1);
                _sawmill.Debug($"Update: applied damage to uid={uid}");
            }

            if (time <= acidCorrodingComponent.AcidExpiresAt)
                continue;

            _sawmill.Debug($"Update: acid expired for uid={uid}, deleting acid={acidCorrodingComponent.Acid}");
            QueueDel(acidCorrodingComponent.Acid);
            RemCompDeferred<AcidCorrodingComponent>(uid);
        }
        if (count > 0)
            _sawmill.Debug($"Update: processed {count} acid corroding components");
    }
}
