using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.FlavorText;

[Serializable, NetSerializable]
public enum ErpStatus : int
{
    No = 0,
    Consentual = 1,
    NonCon = 2
}
