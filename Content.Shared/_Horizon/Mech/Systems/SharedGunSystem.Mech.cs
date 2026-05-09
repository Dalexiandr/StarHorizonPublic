using System.Linq;
using Content.Shared._Horizon.Mech.Equipment.Components;
using Content.Shared._Horizon.Weapons.Ranged.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _sol = default!;

    protected virtual void InitializeMechGun()
    {
        base.Initialize();
        SubscribeLocalEvent<MechGunComponent, ShotAttemptedEvent>(OnShotAttempt);

        SubscribeLocalEvent<BallisticMechAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<BallisticMechAmmoProviderComponent, GetAmmoCountEvent>(OnMechAmmoCount);

        SubscribeLocalEvent<BatteryMechAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<BatteryMechAmmoProviderComponent, GetAmmoCountEvent>(OnMechAmmoCount);

        SubscribeLocalEvent<HitscanMechAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<HitscanMechAmmoProviderComponent, GetAmmoCountEvent>(OnMechAmmoCount);

        SubscribeLocalEvent<SyringeMechGunComponent, StartupMechShootableEvent>(OnSuringeStartup);
    }

    private void OnShotAttempt(EntityUid uid, MechGunComponent comp, ref ShotAttemptedEvent args)
    {
        if (!TryComp<MechComponent>(args.User, out var mech))
        {
            args.Cancel();
            return;
        }

        if (mech.Energy.Float() <= 0f)
            args.Cancel();

        if (TryComp<BallisticMechAmmoProviderComponent>(uid, out var projMech) && projMech.Shots <= 0)
            args.Cancel();
    }

    private void OnMechAmmoCount(EntityUid uid, MechAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        switch (component)
        {
            case BallisticMechAmmoProviderComponent projectile:
                args.Count = projectile.Shots;
                args.Capacity = projectile.Capacity;
                break;
            case BatteryMechAmmoProviderComponent:
                args.Count = 5;
                args.Capacity = 5;
                break;
            case HitscanMechAmmoProviderComponent:
                args.Count = 5;
                args.Capacity = 5;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnSuringeStartup(EntityUid uid, SyringeMechGunComponent component, ref StartupMechShootableEvent args)
    {
        if (!_sol.TryGetSolution(args.Shootable, component.SolutionName, out var solution))
            return;

        _sol.TryAddReagent(solution.Value, component.CurrentReagent, component.Amount);
    }

    private void OnTakeAmmo(EntityUid uid, MechAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var equipmentComp = Comp<MechEquipmentComponent>(uid);

        switch (component)
        {
            case BallisticMechAmmoProviderComponent projectile:
                var shots = Math.Min(args.Shots, projectile.Shots);

                // Don't dirty if it's an empty fire.
                if (shots == 0)
                    return;

                if (projectile.Reloading)
                    return;

                for (var i = 0; i < shots; i++)
                {
                    args.Ammo.Add(GetShootable(uid, projectile, args.Coordinates));
                    projectile.Shots--;
                }
                break;
            case BatteryMechAmmoProviderComponent battery:
                if (args.Shots == 0)
                    return;

                for (var i = 0; i < args.Shots; i++)
                {
                    args.Ammo.Add(GetShootable(uid, battery, args.Coordinates));
                    if (!equipmentComp.EquipmentOwner.HasValue)
                        break;
                    if (!_mech.TryChangeEnergy(equipmentComp.EquipmentOwner.Value, -battery.ShotCost))
                        break;
                }
                break;
            case HitscanMechAmmoProviderComponent hitscan:
                if (args.Shots == 0)
                    return;

                for (var i = 0; i < args.Shots; i++)
                {
                    args.Ammo.Add(GetShootable(uid, hitscan, args.Coordinates));
                    if (!equipmentComp.EquipmentOwner.HasValue)
                        break;
                    if (!_mech.TryChangeEnergy(equipmentComp.EquipmentOwner.Value, -hitscan.ShotCost))
                        break;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_netManager.IsServer)
            Dirty(uid, component);
    }

    private (EntityUid? Entity, IShootable) GetShootable(EntityUid gun, MechAmmoProviderComponent component, EntityCoordinates coordinates)
    {
        switch (component)
        {
            case BallisticMechAmmoProviderComponent proj:
                return StartupMechShootable(gun, proj.Prototype, coordinates);
            case BatteryMechAmmoProviderComponent battery:
                return StartupMechShootable(gun, battery.Prototype, coordinates);
            case HitscanMechAmmoProviderComponent hitscan:
                return (null, ProtoManager.Index(hitscan.Proto));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private (EntityUid? Entity, IShootable) StartupMechShootable(EntityUid gun, EntProtoId proto, EntityCoordinates coordinates)
    {
        var ent = Spawn(proto, coordinates);

        var ev = new StartupMechShootableEvent(ent);
        RaiseLocalEvent(gun, ref ev);

        return (ent, EnsureShootable(ent));
    }
}
