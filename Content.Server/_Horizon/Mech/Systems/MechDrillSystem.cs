using Content.Server.Gatherable;
using Content.Server.Interaction;
using Content.Server.Mech.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Robust.Shared.Audio.Systems;
using Content.Server._Horizon.Mech.Equipment.Components;
using Content.Server.Gatherable.Components;
using Content.Shared.Whitelist;

namespace Content.Server._Horizon.Mech.Equipment.EntitySystems;

/// <summary>
/// Handles <see cref="MechDrillComponent"/> and all related UI logic
/// </summary>
public sealed class MechDrillSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly GatherableSystem _gatherable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechDrillComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<MechDrillComponent, MechDrillDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// When mecha driver uses the tool
    /// </summary>
    private void OnInteract(EntityUid uid, MechDrillComponent component, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        var target = args.Target;

        if (!TryComp<MechComponent>(args.User, out var mech))
            return;

        if (mech.Energy + component.DrillEnergyDelta < 0)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target))
            return;

        args.Handled = true;
        component.Token = new();
        // One tick = one hit; repeating do-after chips the target down. Do not scale delay by total structure HP
        // (that made a single tick take minutes while still applying only one hit of damage).
        var damageTime = 0.35f;
        var doAfter = new DoAfterArgs(EntityManager, args.User, damageTime, new MechDrillDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
        };
        _audio.PlayPvs(component.DrillSound, uid);
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, MechDrillComponent component, MechDrillDoAfterEvent args)
    {
        if (args?.Args?.Target is not { } target)
            return;

        if (args.Cancelled)
            return;
        component.Token = null;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) || equipmentComponent.EquipmentOwner == null)
            return;

        if (!_mech.TryChangeEnergy(equipmentComponent.EquipmentOwner.Value, component.DrillEnergyDelta))
            return;

        var owner = equipmentComponent.EquipmentOwner.Value;

        // Same as pickaxe / PKA: supercompacted and some asteroids only break via Gather when the tool passes whitelist.
        if (TryComp<GatherableComponent>(target, out var gatherable)
            && !_whitelist.IsWhitelistFailOrNull(gatherable.ToolWhitelist, uid))
        {
            _gatherable.Gather(target, uid, gatherable);
            _mech.UpdateUserInterface(owner);
            args.Repeat = Comp<MechComponent>(owner).Energy > 0;
            return;
        }

        _damageable.TryChangeDamage(target, component.DamageToDrilled, ignoreResistances: true);
        _mech.UpdateUserInterface(owner);
        args.Repeat = Comp<MechComponent>(owner).Energy > 0;
    }
}
