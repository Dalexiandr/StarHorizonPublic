using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.RemoteControl;

[Serializable, NetSerializable]
public enum RemoteControlDeviceVisualStates : byte
{
    IsActive
}

public enum RemoteControlDeviceVisualLayers : byte
{
    Indicator
}
