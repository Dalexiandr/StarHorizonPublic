using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.MediaPlayer;

[Prototype("MediaFile")]
public sealed partial class MediaFilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string SongName = string.Empty;

    [DataField(required: true)]
    public string Author = string.Empty;

    [DataField(required: true)]
    public SoundPathSpecifier SoundPath = null!;
}
