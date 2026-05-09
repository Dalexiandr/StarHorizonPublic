using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class CytologyDiskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// Applies disk modifications to a spawned entity.
    /// </summary>
    public void ApplyDiskModifications(EntityUid entity, List<string> selectedDiskPrototypes)
    {
        foreach (var diskProtoId in selectedDiskPrototypes)
        {

            // We take protorype then get IComp from this and finaly CytologyDiskComponent
            if (!_prototypeManager.TryIndex<EntityPrototype>(diskProtoId, out var diskProto) ||
                !diskProto.Components.TryGetComponent("CytologyDisk", out var diskC) ||
                diskC is not CytologyDiskComponent diskComp) // ... °˖✧◝(⁰▿⁰)◜✧˖° 
                continue;

            foreach (var modifier in diskComp.Modifiers)
            {
                ApplyModifier(entity, modifier);
            }
        }
    }

    /// <summary>
    /// Each state needs to be added manually
    /// <seealso cref="DiskParameters"/> You also need to add a state here (⌒_⌒;)
    /// </summary>
    private void ApplyModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        // Part of this can be replaced by generator but for now we have this. If we have more states, we can think about improving them
        switch (modifier.ComponentType)
        {
            case "MovementSpeedModifier":
                ApplyMovementSpeedModifier(entity, modifier);
                break;
            case "MeleeWeaponDamage":
                ApplyMeleeWeaponModifier(entity, modifier);
                break;
            case "MobHealth":
                ApplyMobThresholdsModifier(entity, modifier);
                break;
            case "HungerRate":
                ApplyHungerComponentModifier(entity, modifier);
                break;
            case "Stamina":
                ApplyStaminaModifier(entity, modifier);
                break;
            case "MaxPlasma":
                ApplyMaxPlasmaAmountModifier(entity, modifier);
                break;
            case "PlasmaRechargeSpeed":
                ApplyPlasmaRechargeSpeed(entity, modifier);
                break;
            case "GunFireRate":
                ApplyGunFireRateSpeed(entity, modifier);
                break;
            default:
                break;
        }
    }

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    private void ApplyMovementSpeedModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<MovementSpeedModifierComponent>(entity, out var speedMod))
            return;

        if (modifier.PercentModifier.HasValue)
            _movementSpeed.ChangeBaseSpeed(entity, speedMod.BaseWalkSpeed, speedMod.BaseSprintSpeed * modifier.PercentModifier.Value, speedMod.Acceleration);

    }

    private void ApplyMeleeWeaponModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<MeleeWeaponComponent>(entity, out var meleeWeapon))
            return;

        if (modifier.PercentModifier.HasValue)
        {
            var newDamage = new DamageSpecifier();
            foreach (var (damageType, value) in meleeWeapon.Damage.DamageDict)
            {
                newDamage.DamageDict[damageType] = FixedPoint2.New((float)(value * modifier.PercentModifier.Value));
            }
            meleeWeapon.Damage = newDamage;
            Dirty(entity, meleeWeapon);
        }
    }

    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    private void ApplyMobThresholdsModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<MobThresholdsComponent>(entity, out var thresholds))
            return;

        var thresholdhCrit = _mobThreshold.GetThresholdForState(entity, Mobs.MobState.Critical);
        var thresholdDeath = _mobThreshold.GetThresholdForState(entity, Mobs.MobState.Dead);

        if (modifier.PercentModifier.HasValue)
        {
            thresholdhCrit *= modifier.PercentModifier.Value;
            thresholdDeath *= modifier.PercentModifier.Value;
        }
        else
            return;

        _mobThreshold.SetMobStateThreshold(entity, thresholdhCrit, Mobs.MobState.Critical);
        _mobThreshold.SetMobStateThreshold(entity, thresholdDeath, Mobs.MobState.Dead);
    }

    [Dependency] private readonly HungerSystem _hunger = default!;
    private void ApplyHungerComponentModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<HungerComponent>(entity, out var hunger))
            return;

        if (modifier.PercentModifier.HasValue)
            _hunger.SetBaseDecayRate(entity, hunger.BaseDecayRate * modifier.PercentModifier.Value, hunger);

    }
    private void ApplyStaminaModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<StaminaComponent>(entity, out var stamina))
            return;

        if (modifier.PercentModifier.HasValue)
            stamina.CritThreshold *= modifier.PercentModifier.Value;
    }


    private void ApplyMaxPlasmaAmountModifier(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<PlasmaVesselComponent>(entity, out var plasma))
            return;

        if (modifier.PercentModifier.HasValue)
            plasma.MaxPlasma *= FixedPoint2.New(modifier.PercentModifier.Value);
    }

    private void ApplyPlasmaRechargeSpeed(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<PlasmaVesselComponent>(entity, out var plasma))
            return;

        if (modifier.PercentModifier.HasValue)
        {
            plasma.PlasmaPerSecondOffWeed *= FixedPoint2.New(modifier.PercentModifier.Value);
            plasma.PlasmaPerSecondOnWeed *= FixedPoint2.New(modifier.PercentModifier.Value);
        }
    }

    private void ApplyGunFireRateSpeed(EntityUid entity, CytologyDiskModifier modifier)
    {
        if (!TryComp<GunComponent>(entity, out var gun))
            return;

        if (modifier.PercentModifier.HasValue)
        {
            gun.FireRate *= modifier.PercentModifier.Value;
        }
    }
}

