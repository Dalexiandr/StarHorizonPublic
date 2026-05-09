using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.Cryptominer;

[Serializable, NetSerializable]
public sealed class CryptominerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly CryptominerState State;
    public readonly float CurrentTemperature;
    public readonly float WarningTemperature;
    public readonly float OverheatTemperature;
    public readonly float CriticalTemperature;
    public readonly float CreditsPerMinute;
    public readonly float Efficiency;
    public readonly float PowerConsumption;
    public readonly bool IsPowered;
    public readonly int DiskCount;

    public CryptominerBoundUserInterfaceState(
        CryptominerState state,
        float currentTemperature,
        float warningTemperature,
        float overheatTemperature,
        float criticalTemperature,
        float creditsPerMinute,
        float efficiency,
        float powerConsumption,
        bool isPowered,
        int diskCount)
    {
        State = state;
        CurrentTemperature = currentTemperature;
        WarningTemperature = warningTemperature;
        OverheatTemperature = overheatTemperature;
        CriticalTemperature = criticalTemperature;
        CreditsPerMinute = creditsPerMinute;
        Efficiency = efficiency;
        PowerConsumption = powerConsumption;
        IsPowered = isPowered;
        DiskCount = diskCount;
    }
}

[Serializable, NetSerializable]
public sealed class CryptominerToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum CryptominerUiKey : byte
{
    Key
}
