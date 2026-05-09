using System.Numerics;
using Content.Server._Horizon._Fractions.AnCo.Implants.Components;
using Content.Server.Power.Components;
using Content.Server.Salvage.Expeditions;
using Content.Server.Station.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Horizon._Fractions.AnCo.Implants;

public sealed class AnCoExtractImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoExtractImplantComponent, ActivateImplantEvent>(OnActivate);
        SubscribeLocalEvent<AnCoExtractImplantComponent, ImplantRelayEvent<MobStateChangedEvent>>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, AnCoExtractImplantComponent component, ImplantRelayEvent<MobStateChangedEvent> args)
    {
        if (args.Event.NewMobState != MobState.Dead)
            return;

        if (component.Activated)
            return;

        if (!TryComp<SubdermalImplantComponent>(uid, out var implant) || implant.ImplantedEntity == null)
            return;

        ActivateExtraction(uid, component, implant.ImplantedEntity.Value);
    }

    private void OnActivate(EntityUid uid, AnCoExtractImplantComponent component, ActivateImplantEvent args)
    {
        if (!TryComp<SubdermalImplantComponent>(uid, out var implant) || implant.ImplantedEntity == null)
            return;

        var target = implant.ImplantedEntity.Value;

        if (component.Activated)
            return;

        if (component.KillOnActivate)
        {
            if (!TryComp<MobStateComponent>(target, out var mobState))
                return;

            if (mobState.CurrentState != MobState.Dead)
            {
                _mobState.ChangeMobState(target, MobState.Dead, mobState);
                return;
            }
        }

        ActivateExtraction(uid, component, target);
    }

    private void ActivateExtraction(EntityUid implantUid, AnCoExtractImplantComponent component, EntityUid target)
    {
        if (component.Activated)
            return;

        if (component.ActivationDamage != null)
            _damageable.TryChangeDamage(target, component.ActivationDamage);

        component.Target = target;

        if (component.PackBody)
        {
            var targetXform = Transform(target);
            var bodyBag = Spawn(component.BodyBagPrototype, targetXform.Coordinates);

            var inserted = _entityStorage.Insert(target, bodyBag);

            if (inserted)
            {
                component.BodyBag = bodyBag;
                _audio.PlayPvs(component.TeleportSound, bodyBag);
            }
            else
            {
                QueueDel(bodyBag);
                return;
            }
        }
        else
        {
            component.BodyBag = target;
            _audio.PlayPvs(component.TeleportSound, target);
        }

        if (component.TeleportAnywhere)
        {
            component.TeleportTime = _timing.CurTime + TimeSpan.FromSeconds(component.TeleportDelay);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnCoExtractImplantComponent, SubdermalImplantComponent>();
        while (query.MoveNext(out var implantUid, out var extractComp, out var subdermalComp))
        {
            if (!extractComp.Activated || extractComp.BodyBag == null)
                continue;

            if (!Exists(extractComp.BodyBag.Value) || Deleted(extractComp.BodyBag.Value))
            {
                ResetImplant(extractComp);
                continue;
            }

            if (!TryComp<TransformComponent>(extractComp.BodyBag.Value, out var bodyBagXform))
            {
                ResetImplant(extractComp);
                continue;
            }

            var mapUid = bodyBagXform.MapUid;
            if (mapUid == null)
                continue;

            if (TryComp<SalvageExpeditionComponent>(mapUid.Value, out var expedition))
            {
                if (extractComp.RequireExpeditionFinalStage)
                {
                    var timeRemaining = expedition.EndTime - _timing.CurTime;
                    if (timeRemaining.TotalSeconds <= 5.0)
                    {
                        TeleportToTelepad(implantUid, extractComp, bodyBagXform);
                    }
                }
                else
                {
                    TeleportToTelepad(implantUid, extractComp, bodyBagXform);
                }
            }
            else if (extractComp.TeleportAnywhere)
            {
                if (extractComp.Target == null)
                    continue;

                if (extractComp.TeleportTime != null && _timing.CurTime < extractComp.TeleportTime.Value)
                    continue;

                TeleportToTelepad(implantUid, extractComp, bodyBagXform);
            }
            else
            {
                ResetImplant(extractComp);
                QueueDel(implantUid);
            }
        }
    }

    private void ResetImplant(AnCoExtractImplantComponent component)
    {
        component.BodyBag = null;
        component.Target = null;
        component.TeleportTime = null;
    }

    private void TeleportToTelepad(EntityUid implantUid, AnCoExtractImplantComponent extractComp, TransformComponent bodyBagXform)
    {
        var bodyBagMapUid = bodyBagXform.MapUid;
        if (bodyBagMapUid == null)
            return;

        EntityCoordinates? targetCoords = null;
        float closestDistance = float.MaxValue;
        EntityUid? targetTelepad = null;

        var bodyBagMapPos = _xform.ToMapCoordinates(bodyBagXform.Coordinates);

        if (!string.IsNullOrEmpty(extractComp.TelepadPrototypeId))
        {
            var allEntities = EntityQueryEnumerator<TransformComponent, MetaDataComponent>();
            while (allEntities.MoveNext(out var uid, out var xform, out var meta))
            {
                if (xform.MapUid != bodyBagMapUid)
                    continue;

                if (meta.EntityPrototype?.ID != extractComp.TelepadPrototypeId)
                    continue;

                if (!xform.Anchored)
                    continue;

                if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                    continue;

                var telepadMapPos = _xform.ToMapCoordinates(xform.Coordinates);
                var distance = Vector2.Distance(telepadMapPos.Position, bodyBagMapPos.Position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetCoords = xform.Coordinates;
                    targetTelepad = uid;
                }
            }
        }
        else
        {
            var telepadQuery = EntityQueryEnumerator<AnCoExtractTelepadComponent, TransformComponent, ApcPowerReceiverComponent>();
            while (telepadQuery.MoveNext(out var telepadUid, out _, out var telepadXform, out var powerReceiver))
            {
                if (telepadXform.MapUid != bodyBagMapUid)
                    continue;

                if (!powerReceiver.Powered || !telepadXform.Anchored)
                    continue;

                var telepadMapPos = _xform.ToMapCoordinates(telepadXform.Coordinates);
                var distance = Vector2.Distance(telepadMapPos.Position, bodyBagMapPos.Position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetCoords = telepadXform.Coordinates;
                    targetTelepad = telepadUid;
                }
            }
        }

        if (targetCoords != null && extractComp.BodyBag != null && targetTelepad != null)
        {
            Spawn(extractComp.TeleportEffect, bodyBagXform.Coordinates);

            _xform.SetCoordinates(extractComp.BodyBag.Value, targetCoords.Value);

            if (TryComp<AnCoExtractTelepadComponent>(targetTelepad.Value, out var telepad))
            {
                _audio.PlayPvs(telepad.TeleportSound, targetTelepad.Value);
            }
            else
            {
                _audio.PlayPvs(extractComp.TeleportSound, targetTelepad.Value);
            }

            ResetImplant(extractComp);
            QueueDel(implantUid);
        }
    }
}
