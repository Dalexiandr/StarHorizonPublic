using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.Materials;

/// <summary>
/// Перенесённая из прошлого (16a0957a50d4ae1560235caeba03938dcbc8aa4c) старая система утилизации отходов
/// </summary>
public abstract class SharedHorizonMaterialReclaimerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAmbientSoundSystem AmbientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public const string ActiveReclaimerContainerId = "active-material-reclaimer-container";

    public override void Initialize()
    {
        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ActiveHorizonMaterialReclaimerComponent, ComponentStartup>(OnActiveStartup);
    }

    private void OnMapInit(EntityUid uid, HorizonMaterialReclaimerComponent component, MapInitEvent args)
    {
        component.NextSound = Timing.CurTime;
    }

    private void OnShutdown(EntityUid uid, HorizonMaterialReclaimerComponent component, ComponentShutdown args)
    {
        _audio.Stop(component.Stream);
    }

    private void OnExamined(EntityUid uid, HorizonMaterialReclaimerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("recycler-count-items", ("items", component.ItemsProcessed)));
    }

    private void OnEmagged(EntityUid uid, HorizonMaterialReclaimerComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    private void OnActiveStartup(EntityUid uid, ActiveHorizonMaterialReclaimerComponent component, ComponentStartup args)
    {
        component.ReclaimingContainer = Container.EnsureContainer<Container>(uid, ActiveReclaimerContainerId);
    }

    public bool TryStartProcessItem(EntityUid uid, EntityUid item, HorizonMaterialReclaimerComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanStart(uid, component))
            return false;

        if (HasComp<MobStateComponent>(item) && !CanGib(uid, item, component)) // whitelist? We be gibbing, boy!
            return false;

        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, item) ||
            _whitelistSystem.IsBlacklistPass(component.Blacklist, item))
            return false;

        if (Container.TryGetContainingContainer(item, out _) && !Container.TryRemoveFromContainer(item))
            return false;

        if (user != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(user.Value):player} destroyed {ToPrettyString(item)} in the material reclaimer, {ToPrettyString(uid)}");
        }

        if (Timing.CurTime > component.NextSound)
        {
            component.Stream = _audio.PlayPredicted(component.Sound, uid, user)?.Entity;
            component.NextSound = Timing.CurTime + component.SoundCooldown;
        }

        var reclaimedEvent = new GotReclaimedEvent(Transform(uid).Coordinates);
        RaiseLocalEvent(item, ref reclaimedEvent);

        var duration = GetReclaimingDuration(uid, item, component);
        // if it's instant, don't bother with all the active comp stuff.
        if (duration == TimeSpan.Zero)
        {
            Reclaim(uid, item, 1, component);
            return true;
        }

        var active = EnsureComp<ActiveHorizonMaterialReclaimerComponent>(uid);
        active.Duration = duration;
        active.EndTime = Timing.CurTime + duration;
        Container.Insert(item, active.ReclaimingContainer);
        return true;
    }

    public virtual bool TryFinishProcessItem(EntityUid uid, HorizonMaterialReclaimerComponent? component = null, ActiveHorizonMaterialReclaimerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        RemCompDeferred(uid, active);
        return true;
    }

    public virtual void Reclaim(EntityUid uid,
        EntityUid item,
        float completion = 1f,
        HorizonMaterialReclaimerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ItemsProcessed++;
        if (component.CutOffSound)
        {
            _audio.Stop(component.Stream);
        }

        Dirty(uid, component);
    }

    public bool CanStart(EntityUid uid, HorizonMaterialReclaimerComponent component)
    {
        if (HasComp<ActiveHorizonMaterialReclaimerComponent>(uid))
            return false;

        return component.Powered && component.Enabled;
    }

    public bool CanGib(EntityUid uid, EntityUid victim, HorizonMaterialReclaimerComponent component)
    {
        return component.Powered &&
               component.Enabled &&
               HasComp<BodyComponent>(victim) &&
               HasComp<EmaggedComponent>(uid);
    }

    public TimeSpan GetReclaimingDuration(EntityUid reclaimer,
        EntityUid item,
        HorizonMaterialReclaimerComponent? reclaimerComponent = null,
        PhysicalCompositionComponent? compositionComponent = null)
    {
        if (!Resolve(reclaimer, ref reclaimerComponent))
            return TimeSpan.Zero;

        if (!reclaimerComponent.ScaleProcessSpeed ||
            !Resolve(item, ref compositionComponent, false))
            return reclaimerComponent.MinimumProcessDuration;

        var materialSum = compositionComponent.MaterialComposition.Values.Sum();
        materialSum *= CompOrNull<StackComponent>(item)?.Count ?? 1;
        var duration = TimeSpan.FromSeconds(materialSum / reclaimerComponent.MaterialProcessRate);
        if (duration < reclaimerComponent.MinimumProcessDuration)
            duration = reclaimerComponent.MinimumProcessDuration;
        return duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ActiveHorizonMaterialReclaimerComponent, HorizonMaterialReclaimerComponent>();
        while (query.MoveNext(out var uid, out var active, out var reclaimer))
        {
            if (Timing.CurTime < active.EndTime)
                continue;
            TryFinishProcessItem(uid, reclaimer, active);
        }
    }
}
