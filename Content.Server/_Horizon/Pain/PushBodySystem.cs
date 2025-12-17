using System.Numerics;
using Content.Shared._Horizon.Pain.Components;
using Content.Shared.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using static Robust.Shared.Maths.Direction;

namespace Content.Server._Horizon.Pain;

public sealed class PushBodySystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = null!;
    [Dependency] private readonly IConfigurationManager _cfg = null!;

    public override void Initialize()
    {
        base.Initialize();

        if (!_cfg.GetCVar(CCVars.EnablePushBody)) // Будем ли мы толкать тела?
            return;

        SubscribeLocalEvent<PushableBodyComponent, ProjectileBodyHitEvent>(OnProjectileHitBody);
        SubscribeLocalEvent<PushableBodyComponent, HitScanHitBodyEvent>(OnHitscanHitBody);
        SubscribeLocalEvent<PushableBodyComponent, MeleeHeavyHitBodyAttackEvent>(OnMeleeHeavyHitBody);
    }

    private void OnProjectileHitBody(EntityUid uid, PushableBodyComponent component, ref ProjectileBodyHitEvent ev)
    {
        if (!TryComp<PhysicsComponent>(ev.Bullet, out var physics))
            return;

        SendImpulse(uid, physics.LinearVelocity, physics.Mass, ev.Damage.GetTotal().Float());
    }

    private void OnHitscanHitBody(EntityUid uid, PushableBodyComponent component, ref HitScanHitBodyEvent ev)
    {
        SendImpulse(uid, ev.Direction * 10, 0f, ev.Damage.GetTotal().Float());
    }

    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
    private void OnMeleeHeavyHitBody(EntityUid uid, PushableBodyComponent component, ref MeleeHeavyHitBodyAttackEvent ev)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds) || !ev.Damage.DamageDict.TryGetValue("Blunt", out var bluntDamage))
            return;

        var aliveThres = FixedPoint2.Zero;
        var critThres = FixedPoint2.Zero;
        foreach (var (fix2, mobState) in thresholds.Thresholds)
        {
            switch (mobState)
            {
                case MobState.Alive:
                    aliveThres = fix2;
                    break;
                case MobState.Critical:
                    critThres = fix2;
                    break;
            }
        }

        if (critThres == FixedPoint2.Zero || critThres - aliveThres <= FixedPoint2.Zero)
            return;

        if (critThres.Float() * 0.2f > bluntDamage.Float())
            return;

        SendImpulse(uid, ev.Direction * 50, 0, bluntDamage.Float(), true);
    }

    private void SendImpulse(EntityUid uid, Vector2 impulse, float physicsMass, float totalDamage, bool skipCheck = false)
    {
        if (!TryComp<PhysicsComponent>(uid, out var bodyPhysics))
            return;

        var bodyDir = bodyPhysics.LinearVelocity.GetDir();
        var impulseDir = impulse.GetDir();
        if (bodyPhysics.LinearVelocity == Vector2.Zero || !CheckOppositeDirection(bodyDir, impulseDir)
            && !skipCheck)
        {
            _physicsSystem.ApplyLinearImpulse(uid, impulse / 2f);
            return;
        }

        float mass;
        if (physicsMass == 0)
            mass = 0.5f * totalDamage;
        else
            mass = physicsMass;

        _physicsSystem.ApplyLinearImpulse(uid, impulse * mass);
    }

    // ReSharper disable once ConvertSwitchStatementToSwitchExpression
    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
    private static bool CheckOppositeDirection(Direction direction, Direction oppositeDirection)
    {
        switch (direction)
        {
            case South:
                return oppositeDirection is North or NorthEast or NorthWest;
            case SouthEast:
                return oppositeDirection is North or NorthWest or West;
            case East:
                return oppositeDirection is West or NorthWest or SouthWest;
            case NorthEast:
                return oppositeDirection is SouthWest or South or West;
            case North:
                return oppositeDirection is South or SouthEast or SouthWest;
            case NorthWest:
                return oppositeDirection is SouthEast or South or East;
            case West:
                return oppositeDirection is East or NorthEast or SouthWest;
            case SouthWest:
                return oppositeDirection is North or NorthEast or East;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
        return false;
    }
}
