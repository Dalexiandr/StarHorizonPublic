using Content.Server._Horizon._Fractions.AnCoDisposableFabricator.Systems;
using Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon._Fractions.AnCoDisposableFabricator.Components;

/// <summary>
/// This component stores the possible contents of the fabricator structure,
/// which can be selected via the interface. After approval, plays work animation,
/// spawns items at the structure's position and deletes the structure.
/// </summary>
[RegisterComponent, Access(typeof(AnCoDisposableFabricatorSystem))]
public sealed partial class AnCoDisposableFabricatorComponent : Component
{
    /// <summary>
    /// List of sets available for selection
    /// </summary>
    [DataField]
    public List<ProtoId<AnCoDisposableFabricatorSetPrototype>> PossibleSets = new();

    [DataField]
    public List<int> SelectedSets = new();

    /// <summary>
    /// Sound played during the work animation.
    /// </summary>
    [DataField]
    public SoundPathSpecifier WorkingSound = new SoundPathSpecifier("/Audio/Effects/hydraulic_press.ogg");

    /// <summary>
    /// Max number of sets you can select.
    /// </summary>
    [DataField]
    public int MaxSelectedSets = 2;

    /// <summary>
    /// Duration of the work animation in seconds before spawning items.
    /// </summary>
    [DataField]
    public float WorkDuration = 3.3f;

    /// <summary>
    /// The sprite state to use when idle.
    /// </summary>
    [DataField]
    public string IdleState = "icon";

    /// <summary>
    /// The sprite state to use when working.
    /// </summary>
    [DataField]
    public string WorkingState = "work";

    /// <summary>
    /// Whether the fabricator is currently working (playing animation).
    /// </summary>
    [ViewVariables]
    public bool IsWorking;

    /// <summary>
    /// Time when work will finish.
    /// </summary>
    [ViewVariables]
    public TimeSpan? WorkEndTime;
}
