using Content.Server.NPC.Systems;
using Content.Server.NPC.Components; // Добавь эту строку
using Content.Shared._Horizon.NPC;
using Content.Shared.Mobs.Systems;
using Content.Server.Movement.Systems;
using Robust.Shared.Player;

namespace Content.Server._Horizon.NPC
{
    public sealed class LemirdFollowSystem : EntitySystem
    {
        [Dependency] private readonly NPCSteeringSystem _steering = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        private const float DetectionRange = 10f; // Радиус обнаружения игроков

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LemirdFollowComponent, ComponentStartup>(OnFollowStartup);
            SubscribeLocalEvent<LemirdFollowComponent, ComponentShutdown>(OnFollowShutdown);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<LemirdFollowComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var follow, out var transform))
            {
                UpdateLemird(uid, follow, transform, frameTime);
            }
        }

        private void UpdateLemird(EntityUid uid, LemirdFollowComponent follow, TransformComponent transform, float frameTime)
        {
            // Проверяем, жив ли лемирд
            if (EntityManager.IsQueuedForDeletion(uid) ||
                !EntityManager.EntityExists(uid) ||
                _mobState.IsDead(uid))
            {
                StopFollowing(uid, follow);
                return;
            }

            // Если уже нашел цель и она валидна - следуем
            if (follow.HasFoundFirstTarget && follow.Target != null)
            {
                var target = follow.Target.Value;

                if (!ValidateTarget(target))
                {
                    // Цель невалидна, но не ищем новую (onlyFirstTarget = true)
                    follow.Target = null;
                    StopFollowing(uid, follow);
                    return;
                }

                FollowTarget(uid, follow, target, transform);
                return;
            }

            // Если еще не нашел цель - ищем первую
            if (!follow.HasFoundFirstTarget)
            {
                FindFirstTarget(uid, follow, transform);
            }
        }

        private bool ValidateTarget(EntityUid target)
        {
            return EntityManager.EntityExists(target) &&
                   !EntityManager.IsQueuedForDeletion(target) &&
                   !_mobState.IsDead(target) &&
                   HasComp<ActorComponent>(target);
        }

        private void FindFirstTarget(EntityUid uid, LemirdFollowComponent follow, TransformComponent transform)
        {
            EntityUid? closestPlayer = null;
            float closestDistance = float.MaxValue;

            // Ищем всех игроков на карте
            var playerQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();

            while (playerQuery.MoveNext(out var playerUid, out var actor, out var playerTransform))
            {
                // Пропускаем себя
                if (playerUid == uid)
                    continue;

                // Проверяем, что игрок жив
                if (_mobState.IsDead(playerUid) || EntityManager.IsQueuedForDeletion(playerUid))
                    continue;

                // Проверяем расстояние
                var distance = (playerTransform.WorldPosition - transform.WorldPosition).Length();
                if (distance > DetectionRange)
                    continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = playerUid;
                }
            }

            if (closestPlayer != null)
            {
                // Нашли первую цель!
                follow.Target = closestPlayer.Value;
                follow.HasFoundFirstTarget = true;

                // Начинаем следовать
                FollowTarget(uid, follow, closestPlayer.Value, transform);
            }
        }

        private void FollowTarget(EntityUid uid, LemirdFollowComponent follow, EntityUid target, TransformComponent transform)
        {
            // Проверяем расстояние - если слишком далеко, отключаем следование
            var targetTransform = Transform(target);
            var distance = (targetTransform.WorldPosition - transform.WorldPosition).Length();

            if (distance > follow.MaxFollowDistance)
            {
                // Слишком далеко - останавливаемся
                StopFollowing(uid, follow);
                return;
            }

            // Используем NPC Steering System для движения
            var targetCoords = targetTransform.Coordinates;

            // Проверяем, есть ли уже Steering для этой цели
            if (TryComp<NPCSteeringComponent>(uid, out var steering) &&
                steering.CurrentPath != null &&
                steering.Coordinates.Equals(targetCoords))
            {
                // Уже следуем к этой цели, обновляем параметры
                steering.Range = follow.FollowDistance;
                steering.RepathRange = follow.FollowDistance * 1.5f;
                steering.ArriveOnLineOfSight = false;
                return;
            }

            // Регистрируем новую навигацию
            var newSteering = _steering.Register(uid, targetCoords);

            // Настраиваем параметры следования из компонента
            newSteering.Range = follow.FollowDistance;
            newSteering.RepathRange = follow.FollowDistance * 1.5f;
            newSteering.ArriveOnLineOfSight = false;
        }

        private void StopFollowing(EntityUid uid, LemirdFollowComponent? follow = null)
        {
            if (!Resolve(uid, ref follow))
                return;

            // Отключаем навигацию
            _steering.Unregister(uid);
        }

        private void OnFollowStartup(EntityUid uid, LemirdFollowComponent component, ComponentStartup args)
        {
            // Инициализация - еще не нашел цель
            component.HasFoundFirstTarget = false;
            component.Target = null;
        }

        private void OnFollowShutdown(EntityUid uid, LemirdFollowComponent component, ComponentShutdown args)
        {
            // Отключаем навигацию при удалении компонента
            _steering.Unregister(uid);
        }
    }
}
