using Content.Server._Horizon.AnCoEntityAutoTarget.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server._Horizon._Fractions.AnCoEntityAutoTarget.Systems;

/// <summary>
/// Система отвечающая за то, чтобы добавить пулям оружия компонент автоматического наведения.
/// При выстреле из оружия, вылетевшие пули получают компонент AnCoGunGuidedBullet с настройками
/// оружия.
/// </summary>
public sealed class AnCoEntityAutoTargetSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _state = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Подпись на событие выстрела
        SubscribeLocalEvent<AnCoGunAutoTargetComponent, AmmoShotEvent>(OnAmmoShot);
    }

    // Слушатель события который выдаёт пуле компонент автонаводки
    public void OnAmmoShot(EntityUid uid, AnCoGunAutoTargetComponent component, AmmoShotEvent args)
    {
        var shooter = Transform(uid).ParentUid;

        foreach (var projectile in args.FiredProjectiles)
        {
            var bullet = EnsureComp<AnCoAutoGuidedBulletComponent>(projectile);

            bullet.Range = component.Range;
            bullet.TurnSpeed = component.TurnSpeed;
            bullet.Shooter = shooter;
        }
    }

    // Основная логика наводки пули
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnCoAutoGuidedBulletComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var bullet, out var physics, out var transform))
        {
            var bulletPos = _transform.GetWorldPosition(transform);

            if (bullet.Target == null || Deleted(bullet.Target))
            {
                bullet.Target = FindTarget(uid, bullet, transform, bullet.Range);
                if (bullet.Target == null) continue;
            }

            var targetPos = _transform.GetWorldPosition(bullet.Target.Value);

            var desiredDirection = (targetPos - bulletPos).Normalized();
            var currentVelocity = physics.LinearVelocity;
            var speed = currentVelocity.Length();

            if (speed <= 1f) continue;

            // Поворот вектора пули
            var newVelocity = Vector2.Lerp(currentVelocity.Normalized(), desiredDirection, bullet.TurnSpeed * frameTime).Normalized();

            _physics.SetLinearVelocity(uid, newVelocity * speed, body: physics);

            // Визуальный поворот пули
            transform.LocalRotation = newVelocity.ToWorldAngle();
        }
    }

    private EntityUid? FindTarget(EntityUid entity, AnCoAutoGuidedBulletComponent component, TransformComponent transform, float range)
    {
        var bulletPos = _transform.GetWorldPosition(transform);
        EntityUid? closestTarget = null;
        var minDistance = range;

        if (!(range > 0.01f))
            return null;

        var entities = _lookup.GetEntitiesInRange(transform.MapPosition, range);

        foreach (var target in entities)
        {
            if (target == entity || target == component.Shooter)
                continue;

            if (!HasComp<MindContainerComponent>(target) || !HasComp<MobStateComponent>(target))
                continue;

            if (!_state.IsAlive(target))
                continue;

            var dist = (bulletPos - _transform.GetWorldPosition(target)).Length();
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTarget = target;
            }
        }

        return closestTarget;
    }
}
