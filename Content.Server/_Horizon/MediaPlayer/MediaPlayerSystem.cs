using System.Linq;
using Content.Shared._Horizon.MediaPlayer;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.MediaPlayer;

public sealed class MediaPlayerSystem : SharedMediaPlayerSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = null!;
    private List<MediaFilePrototype> _mediaFilePrototypes = [];
    private readonly Dictionary<EntityUid, string> _mediaById = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MediaPlayerComponent, MediaPlayMessage>(OnMediaPlay);
        SubscribeLocalEvent<MediaPlayerComponent, MediaPauseMessage>(OnMediaPause);
        SubscribeLocalEvent<MediaPlayerComponent, MediaStopMessage>(Stop);

        SubscribeLocalEvent<MediaPlayerComponent, MediaSetTimeMessage>(OnMediaSetTime);
        SubscribeLocalEvent<MediaPlayerComponent, MediaRepeatMessage>(SetRepeatState);
        SubscribeLocalEvent<MediaPlayerComponent, MediaVolumeMessage>(SetMediaVolume);

        //SubscribeLocalEvent<MediaPlayerComponent, PowerChangedEvent>(OnPowerChanged);
        //SubscribeLocalEvent<MediaPlayerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MediaPlayerComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<MediaPlayerComponent, MediaSelectedMessage>(OnMediaSelected);

        _mediaFilePrototypes = _protoManager.EnumeratePrototypes<MediaFilePrototype>()
            .OrderBy(x => x.ID)
            .ToList();
        _protoManager.PrototypesReloaded += _ =>
        {
            _mediaFilePrototypes = _protoManager.EnumeratePrototypes<MediaFilePrototype>()
                .OrderBy(x => x.ID)
                .ToList();
        };
    }

    #region Apperance

    private void OnComponentShutdown(EntityUid uid, MediaPlayerComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
        _mediaById.Remove(uid);
    }

    /*
    private void OnComponentInit(EntityUid uid, MediaPlayerComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnPowerChanged(Entity<MediaPlayerComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void DirectSetVisualState(EntityUid uid, MediaPlayerVisualStates state)
    {
        _appearanceSystem.SetData(uid, MediaPlayerVisuals.VisualState, state);
    }


     private void TryUpdateVisualState(EntityUid uid, MediaPlayerComponent? mediaComponent = null)
    {
        if (!Resolve(uid, ref mediaComponent))
            return;

        var finalState = MediaPlayerVisualStates.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = MediaPlayerVisualStates.Off;
        }

        _appearanceSystem.SetData(uid, MediaPlayerVisuals.VisualState, finalState);
    }
    */

    private void Stop(EntityUid uid, MediaPlayerComponent comp, MediaStopMessage args)
    {
        Audio.Stop(comp.AudioStream);
        comp.SelectedSongId = null;
        comp.AudioStream = null;
        if (!args.Repeat)
            _mediaById.Remove(uid);
        Dirty(uid, comp);
    }
    #endregion

    #region Audio
    private void OnMediaPlay(EntityUid uid, MediaPlayerComponent component, MediaPlayMessage args = null!)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing); //Stop(new Entity<MediaPlayerComponent>(uid, component));
            return;
        }

        if (string.IsNullOrEmpty(component.SelectedSongId))
        {
            return;
        }

        var param = new AudioParams
        {
            Volume = component.Volume,
            Pitch = 1,
            MaxDistance = 15f + component.Volume, // По умолчанию это расстояние будет равно 5f
            RolloffFactor = 1,
            ReferenceDistance = 1,
            Loop = false,
            PlayOffsetSeconds = 0f,
        };

        component.AudioStream = Audio.PlayPvs(component.AudioPath, uid, param)?.Entity;
        Dirty(uid, component);
    }

    private void SetMediaVolume(EntityUid uid, MediaPlayerComponent component, ref MediaVolumeMessage args)
    {
        component.Volume = args.Volume;
        Dirty(uid, component);

        if (!Exists(component.AudioStream))
            return;

        Audio.SetVolume(component.AudioStream, args.Volume);
    }

    private void SetRepeatState(EntityUid uid, MediaPlayerComponent component, ref MediaRepeatMessage args)
    {
        component.Repeat = args.Type;
        Dirty(uid, component);
    }

    private void OnMediaPause(Entity<MediaPlayerComponent> ent, ref MediaPauseMessage _)
    {
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnMediaSetTime(EntityUid uid, MediaPlayerComponent component, ref MediaSetTimeMessage args)
    {
        if (!TryComp(args.Actor, out ActorComponent? actorComp))
            return;

        var offset = actorComp.PlayerSession.Channel.Ping * 2 / 1000f;
        Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
    }

    private void OnMediaSelected(EntityUid uid, MediaPlayerComponent component, ref MediaSelectedMessage args)
    {
        if (args.AudioPath is null)
            return;

        if (Audio.IsPlaying(component.AudioStream))
            Stop(uid, component, new MediaStopMessage(false));

        // Sound settings
        if (args.SongId is null)
            return;

        component.Volume = args.Volume;
        component.SelectedSongId = args.SongId;
        component.AudioPath = args.AudioPath;
        _mediaById[uid] = args.SongId;

        //DirectSetVisualState(uid, MediaPlayerVisualStates.Select);
        OnMediaPlay(uid, component);

        Dirty(uid, component);
    }

    private void HandleRepeat()
    {
        foreach (var (uid, id) in _mediaById)
        {
            if (!TryComp(uid, out MediaPlayerComponent? component))
                continue;

            if (Exists(component.AudioStream) || component.Repeat == RepeatType.None)
                continue;

            using var enumerator = _mediaFilePrototypes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (current.ID != id)
                    continue;

                switch (component.Repeat)
                {
                    case RepeatType.Playlist:
                        current = enumerator.MoveNext() ? enumerator.Current : _mediaFilePrototypes.First();
                        _mediaById[uid] = current.ID;
                        break;
                    case RepeatType.Single:
                        break;
                    case RepeatType.None:
                    default:
                        return;
                }

                component.SelectedSongId = current.ID;
                component.AudioPath = current.SoundPath;
                OnMediaPlay(uid, component);
                Dirty(uid, component);
                RaiseNetworkEvent(new RepeatMessage(GetNetEntity(uid), current.ID));
                break;
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        HandleRepeat();
    }

    #endregion
}
