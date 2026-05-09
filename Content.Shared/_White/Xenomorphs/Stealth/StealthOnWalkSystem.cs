using Content.Shared.Movement.Events;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Log;

namespace Content.Shared._White.Xenomorphs.Stealth;

public sealed class StealthOnWalkSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.stealthonwalk");
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill.Debug("StealthOnWalkSystem initialized");
        SubscribeLocalEvent<StealthOnWalkComponent, SprintingInputEvent>(OnSprintingInput);
    }

    private void OnSprintingInput(EntityUid uid, StealthOnWalkComponent component, SprintingInputEvent args)
    {
        _sawmill.Debug($"OnSprintingInput: uid={uid}, sprinting={args.Entity.Comp.Sprinting}");
        if (!TryComp<StealthComponent>(uid, out var stealth) || stealth.Enabled == !args.Entity.Comp.Sprinting)
            return;

        _stealth.SetEnabled(uid, !args.Entity.Comp.Sprinting, stealth);
        component.Stealth = stealth.Enabled;
        _sawmill.Debug($"OnSprintingInput: set stealth to {stealth.Enabled}");
    }
}
