using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Silicon;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BorgSkin
{
    /// <summary>
    /// Skin name
    /// </summary>
    [DataField]
    public string Name { get; set; } = "Default";

    /// <summary>
    /// The sprite state for the main borg body.
    /// </summary>
    [DataField]
    public string SpriteBodyState { get; set; } = "robot";

    /// <summary>
    /// An optional movement sprite state for the main borg body.
    /// </summary>
    [DataField]
    public string? SpriteBodyMovementState { get; set; }

    /// <summary>
    /// Sprite state used to indicate that the borg has a mind in it.
    /// </summary>
    /// <seealso cref="BorgChassisComponent.HasMindState"/>
    [DataField]
    public string SpriteHasMindState { get; set; } = "robot_e";

    /// <summary>
    /// Sprite state used to indicate that the borg has no mind in it.
    /// </summary>
    /// <seealso cref="BorgChassisComponent.NoMindState"/>
    [DataField]
    public string SpriteNoMindState { get; set; } = "robot_e_r";

    /// <summary>
    /// Sprite state used when the borg's flashlight is on.
    /// </summary>
    [DataField]
    public string SpriteToggleLightState { get; set; } = "robot_l";

    /// <summary>
    /// String to use on petting success.
    /// </summary>
    /// <seealso cref="InteractionPopupComponent"/>
    [DataField]
    public string PetSuccessString { get; set; } = "petting-success-generic-cyborg";

    /// <summary>
    /// String to use on petting failure.
    /// </summary>
    /// <seealso cref="InteractionPopupComponent"/>
    [DataField]
    public string PetFailureString { get; set; } = "petting-failure-generic-cyborg";

    /// <summary>
    /// Additional components to add to the borg entity when this type is selected.
    /// </summary>
    [NonSerialized]
    [DataField(serverOnly: true)]
    public ComponentRegistry? AddComponents;

    /// <summary>
    /// Sound specifier for footstep sounds created by this borg.
    /// </summary>
    [DataField]
    public SoundSpecifier FootstepCollection { get; set; } = new SoundCollectionSpecifier("FootstepBorg");
}
