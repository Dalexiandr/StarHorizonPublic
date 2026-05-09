using Content.Shared._Horizon.MediaPlayer;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Horizon.MediaPlayer;

[UsedImplicitly]
public sealed class MediaPlayerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = null!;
    [Dependency] private readonly IEntityManager _entityManager = null!;

    [ViewVariables]
    private MediaPlayerMenu? _menu;

    private readonly MediaPlayerSystem? _mediaPlayerSystem;
    public MediaPlayerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _mediaPlayerSystem = _entityManager.System<MediaPlayerSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (_mediaPlayerSystem == null)
            return;

        // Create and set setting of menu
        _menu = this.CreateWindow<MediaPlayerMenu>();

        // Logic
        _menu.OnPlayPressed += args =>
        {
            if (args)
                SendMessage(new MediaPlayMessage());
            else
                SendMessage(new MediaPauseMessage());
        }; // Пауза или проигрывание УЖЕ играющего файла.
        _menu.SetTime += args => SendMessage(new MediaSetTimeMessage(args)); // Слайдер таймер.
        _menu.OnStopPressed += SendMessage; // Останавливает музыку полностью
        _menu.OnFileSelected += SendMessage; // Заставляет начать проигрываться файл
        _menu.OnRepeatPressed += SendMessage; // Заставляет файл повторяться во время или до проигрывания, по его окончании
        _menu.OnVolumePressed += SendMessage; // Заставляет файл воиспроизводится с тихо\нормально\громко

        _menu.SetTime += SetTime;
        PopulateMediaList(_mediaPlayerSystem.MediaFilePrototypes);
        Reload(true);
    }

    public void PopulateMediaList(List<MediaFilePrototype> files)
    {
        _menu?.PopulateMediaList(files);
    }

    public void HandleTitleChange(string id)
    {
        _menu?.ChangeTitle(id);
    }

    public void Reload(bool needUpdate = false)
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out MediaPlayerComponent? media))
            return;

        _menu.Audio = media.AudioStream;

        if (_protoManager.TryIndex(media.SelectedSongId, out var songProto))
        {
            var audioSystem = EntMan.System<AudioSystem>();
            var resolvedSound = audioSystem.ResolveSound(songProto.SoundPath);
            var length = audioSystem.GetAudioLength(resolvedSound);

            _menu.SetSliderLength((float)length.TotalSeconds);

            if (needUpdate)
                _menu.UpdateState(songProto.ID, media.Repeat, media.Volume);
        }
        else
        {
            _menu.SetSliderLength(100f);
            if (needUpdate)
                _menu.UpdateState(string.Empty, media.Repeat, media.Volume);
        }
    }

    private void SetTime(float time)
    {
        if (EntMan.TryGetComponent(Owner, out MediaPlayerComponent? media) &&
            EntMan.TryGetComponent(media.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.PlaybackPosition = time;
        }

        SendMessage(new MediaSetTimeMessage(time));
    }
}
