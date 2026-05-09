using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.GhostSprites;

/// <summary>
/// Component that stores the currently selected ghost sprite.
/// Added to ghost entities to track their customized appearance.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class GhostSpriteComponent : Component
{
    /// <summary>
    /// Currently selected ghost sprite prototype ID.
    /// Null means default sprite.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<GhostSpritePrototype>? SelectedSprite;
}
