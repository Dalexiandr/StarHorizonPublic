using Content.Server.NPC.Systems;
using Content.Shared._Horizon.NPC;
using Robust.Shared.Map.Components;
using Content.Server.Movement.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Horizon.NPC
{
    public sealed class FollowSystem : EntitySystem
    {
        [Dependency] private readonly NPCSteeringSystem _steering = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FollowComponent, ComponentStartup>(OnFollowStartup);
            SubscribeLocalEvent<FollowComponent, ComponentShutdown>(OnFollowShutdown);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<FollowComponent>();
            while (query.MoveNext(out var uid, out var follow))
            {
                UpdateFollowing(uid, follow, frameTime);
            }
        }

        private void UpdateFollowing(EntityUid uid, FollowComponent follow, float frameTime)
        {
            // Проверяем, жив ли моб
            if (EntityManager.IsQueuedForDeletion(uid) ||
                !EntityManager.EntityExists(uid) ||
                _mobState.IsDead(uid))
            {
                StopFollowing(uid, follow);
                return;
            }

            // Проверяем цель
            if (follow.Target == null ||
                !EntityManager.EntityExists(follow.Target.Value) ||
                EntityManager.IsQueuedForDeletion(follow.Target.Value))
            {
                StopFollowing(uid, follow);
                return;
            }

            var target = follow.Target.Value;

            // Проверяем расстояние
            var transform = Transform(uid);
            var targetTransform = Transform(target);

            if (follow.StopFollowingIfTooFar &&
                TryCalculateDistance(transform, targetTransform, out var distance) &&
                distance > follow.MaxFollowDistance)
            {
                StopFollowing(uid, follow);
                return;
            }

            // Используем NPC Steering System для движения
            var targetCoords = EntityManager.GetComponent<TransformComponent>(target).Coordinates;

            // Регистрируем моба в системе навигации
            var steering = _steering.Register(uid, targetCoords);

            // Настраиваем параметры следования
            steering.Range = follow.FollowDistance;
            steering.RepathRange = follow.FollowDistance * 1.5f;

            // Отключаем требование прямой видимости для следования
            steering.ArriveOnLineOfSight = false;
        }

        private void StopFollowing(EntityUid uid, FollowComponent? follow = null)
        {
            if (!Resolve(uid, ref follow))
                return;

            // Отключаем навигацию
            _steering.Unregister(uid);

            // Сбрасываем состояние диалога
            if (TryComp<DialogueStateComponent>(uid, out var state))
            {
                state.State = DialogueState.Idle;
                state.CurrentResponse = null;
            }

            // Удаляем компонент следования
            RemCompDeferred<FollowComponent>(uid);
        }

        private void OnFollowStartup(EntityUid uid, FollowComponent component, ComponentStartup args)
        {
            if (TryComp<DialogueStateComponent>(uid, out var state))
            {
                state.State = DialogueState.Following;
            }
        }

        private void OnFollowShutdown(EntityUid uid, FollowComponent component, ComponentShutdown args)
        {
            // Отключаем навигацию при удалении компонента
            _steering.Unregister(uid);

            if (TryComp<DialogueStateComponent>(uid, out var state))
            {
                state.State = DialogueState.Idle;
                state.CurrentResponse = null;
            }
        }

        private bool TryCalculateDistance(TransformComponent transformA, TransformComponent transformB, out float distance)
        {
            distance = 0f;

            if (transformA.MapUid != transformB.MapUid)
                return false;

            var posA = _transform.GetWorldPosition(transformA);
            var posB = _transform.GetWorldPosition(transformB);

            distance = (posB - posA).Length();
            return true;
        }
    }
}
