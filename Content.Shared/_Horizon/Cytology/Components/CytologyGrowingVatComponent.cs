using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Cytology.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CytologyGrowingVatComponent : Component
{
    [DataField]
    public ItemSlot PetriDishSlot = new();

    [DataField]
    public ItemSlot BeakerSlot = new();

    /// <summary>
    ///     A toggle state that indicates whether the vat will work
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsActive = false;

    /// <summary>
    ///     Indicates that an error occurred during cell cultivation. Responsible for switching the indicator
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool StopWithError = false;

    /// <summary>
    ///     An auxiliary field for showing foam
    /// </summary>
    [DataField]
    public bool WithFoam = false;

    [DataField, AutoNetworkedField]
    public bool IsPowered = false;

    [DataField]
    public EntProtoId SmokePrototype = new();

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField, AutoPausedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);

}
