using Robust.Shared.GameStates;

namespace Content.Shared._Horizon._Fractions.AnCo.Cryptominer;

/// <summary>
/// Component for storing mined credits on a disk.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CryptominerDiskComponent : Component
{
    /// <summary>
    /// Current amount of credits stored on this disk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int StoredCredits;

    /// <summary>
    /// Maximum amount of credits that can be stored on this disk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxCredits = 10000;
}
