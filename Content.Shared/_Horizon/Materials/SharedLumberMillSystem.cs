using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.Materials;

/// <summary>
/// Handles interactions and timing for <see cref="LumberMillComponent"/> and <see cref="ActiveLumberMillComponent"/>.
/// </summary>
public abstract class SharedLumberMillSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAmbientSoundSystem AmbientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public const string ActiveLumberMillContainerId = "active-lumber-mill-container";

    public override void Initialize()
    {
        SubscribeLocalEvent<LumberMillComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LumberMillComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LumberMillComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ActiveLumberMillComponent, ComponentStartup>(OnActiveStartup);
    }

    private void OnMapInit(EntityUid uid, LumberMillComponent component, MapInitEvent args)
    {
        component.NextSound = Timing.CurTime;
    }

    private void OnShutdown(EntityUid uid, LumberMillComponent component, ComponentShutdown args)
    {
        _audio.Stop(component.Stream);
    }

    private void OnExamined(EntityUid uid, LumberMillComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("lumber-mill-count-items", ("count", component.ItemsProcessed)));
    }

    private void OnActiveStartup(EntityUid uid, ActiveLumberMillComponent component, ComponentStartup args)
    {
        component.ProcessingContainer = Container.EnsureContainer<Container>(uid, ActiveLumberMillContainerId);
    }

    public bool TryStartProcessItem(EntityUid uid, EntityUid item, LumberMillComponent? component = null, EntityUid? user = null, bool predictSound = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanStart(uid, component))
            return false;

        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, item) ||
            _whitelistSystem.IsBlacklistPass(component.Blacklist, item))
            return false;

        if (Container.TryGetContainingContainer(item, out _) && !Container.TryRemoveFromContainer(item))
            return false;

        if (user != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user.Value):player} inserted {ToPrettyString(item)} into lumber mill {ToPrettyString(uid)}");
        }

        if (Timing.CurTime > component.NextSound)
        {
            if (component.Stream != null)
                _audio.Stop(component.Stream);

            if (predictSound)
                component.Stream = _audio.PlayPredicted(component.Sound, uid, user)?.Entity;
            else
                component.Stream = _audio.PlayPvs(component.Sound, uid)?.Entity;
            component.NextSound = Timing.CurTime + component.SoundCooldown;
        }

        var duration = component.ProcessDuration;
        if (duration == TimeSpan.Zero)
        {
            FinishProcessAndSpawnOutput(uid, item, 1f, component);
            return true;
        }

        var active = EnsureComp<ActiveLumberMillComponent>(uid);
        active.Duration = duration;
        active.EndTime = Timing.CurTime + duration;
        Container.Insert(item, active.ProcessingContainer);
        return true;
    }

    /// <summary>
    /// Called when processing timer ends. Server spawns output from Log component and deletes the item.
    /// </summary>
    public virtual void FinishProcessAndSpawnOutput(EntityUid uid, EntityUid item, float completion, LumberMillComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ItemsProcessed++;
        if (component.CutOffSound)
            _audio.Stop(component.Stream);
        Dirty(uid, component);
    }

    public virtual bool TryFinishProcessItem(EntityUid uid, LumberMillComponent? component = null, ActiveLumberMillComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        var contained = active.ProcessingContainer.ContainedEntities;
        if (contained.Count == 0)
        {
            RemCompDeferred(uid, active);
            return true;
        }
        var item = contained[0];

        Container.Remove(item, active.ProcessingContainer);
        RemCompDeferred(uid, active);
        Dirty(uid, component);

        var completion = 1f;
        if (active.Duration > TimeSpan.Zero)
        {
            var remaining = (float)(active.EndTime - Timing.CurTime).TotalSeconds;
            completion = 1f - Math.Clamp(remaining / (float)active.Duration.TotalSeconds, 0f, 1f);
        }
        FinishProcessAndSpawnOutput(uid, item, completion, component);
        return true;
    }

    public bool CanStart(EntityUid uid, LumberMillComponent component)
    {
        if (HasComp<ActiveLumberMillComponent>(uid))
            return false;
        return component.Powered && component.Enabled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ActiveLumberMillComponent, LumberMillComponent>();
        while (query.MoveNext(out var uid, out var active, out var mill))
        {
            if (Timing.CurTime < active.EndTime)
                continue;
            TryFinishProcessItem(uid, mill, active);
        }
    }
}
