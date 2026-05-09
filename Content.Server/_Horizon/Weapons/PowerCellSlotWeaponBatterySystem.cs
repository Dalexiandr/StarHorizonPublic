using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._Horizon.Weapons;


public sealed class PowerCellSlotWeaponBatterySystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellSlotComponent, GetChargeEvent>(OnGetCharge);
        SubscribeLocalEvent<PowerCellSlotComponent, ChangeChargeEvent>(OnChangeCharge);
    }


    private bool IsBatteryAmmoGun(EntityUid uid)
    {
        return HasComp<HitscanBatteryAmmoProviderComponent>(uid)
               || HasComp<ProjectileBatteryAmmoProviderComponent>(uid);
    }

    private void OnGetCharge(EntityUid uid, PowerCellSlotComponent component, ref GetChargeEvent args)
    {
        if (HasComp<BatteryComponent>(uid))
            return;

        if (!IsBatteryAmmoGun(uid))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out _, out var battery, component))
            return;

        args.CurrentCharge += battery.CurrentCharge;
        args.MaxCharge += battery.MaxCharge;
    }

    private void OnChangeCharge(EntityUid uid, PowerCellSlotComponent component, ref ChangeChargeEvent args)
    {
        if (HasComp<BatteryComponent>(uid))
            return;

        if (!IsBatteryAmmoGun(uid))
            return;

        if (args.ResidualValue == 0f)
            return;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out var batteryEnt, out var battery, component))
            return;

        var applied = _battery.ChangeCharge(batteryEnt.Value, args.ResidualValue, battery);
        args.ResidualValue -= applied;
    }
}
