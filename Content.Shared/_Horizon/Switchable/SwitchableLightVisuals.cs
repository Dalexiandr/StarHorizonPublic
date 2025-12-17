using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Switchable;

// Appearance Data key
[Serializable, NetSerializable]
public enum SwitchableLightVisuals : byte
{
    Enabled,
    Color
}
