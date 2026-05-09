using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.Cryptominer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnCoCryptominerComponent : Component
{
    /// <summary>
    /// Credits generated per minute per disk.
    /// Total credits = BaseCreditsPerMinute * DiskCount
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseCreditsPerMinute = 50;

    /// <summary>
    /// Timer for credit generation (counts up to 60 seconds).
    /// </summary>
    [ViewVariables]
    public float CreditGenerationTimer;

    /// <summary>
    /// Power consumption in watts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PowerConsumption = 500f;

    /// <summary>
    /// Heat energy added to the atmosphere per second per disk in joules.
    /// Total heat = BaseHeatEnergyPerSecond * DiskCount
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseHeatEnergyPerSecond = 5000f;

    /// <summary>
    /// Temperature at which the miner shows a warning state (100°C).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WarningTemperature = 373.15f;

    /// <summary>
    /// Temperature at which the miner starts overheating and reduces efficiency (150°C).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OverheatTemperature = 423.15f;

    /// <summary>
    /// Temperature at which the miner shuts down to prevent damage (200°C).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CriticalTemperature = 473.15f;

    /// <summary>
    /// Current state of the cryptominer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CryptominerState State = CryptominerState.Off;

    /// <summary>
    /// Current environment temperature.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float CurrentTemperature;

    /// <summary>
    /// Efficiency multiplier based on temperature (1.0 = 100%, 0.0 = 0%).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Efficiency = 1.0f;

    /// <summary>
    /// Accumulated fractional credits for precise calculation.
    /// </summary>
    [ViewVariables]
    public float AccumulatedCredits;


    /// <summary>
    /// Number of disk drives currently inserted (0-4).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DiskCount;

    /// <summary>
    /// Minimum atmospheric pressure required to operate (in kPa).
    /// Default is 20 kPa (about 20% of standard atmosphere).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinimumPressure = 20f;

    /// <summary>
    /// Current atmospheric pressure around the miner (in kPa).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float CurrentPressure;

    /// <summary>
    /// Slot names for disk drives.
    /// </summary>
    public const string DiskSlot1 = "disk_slot_1";
    public const string DiskSlot2 = "disk_slot_2";
    public const string DiskSlot3 = "disk_slot_3";
    public const string DiskSlot4 = "disk_slot_4";
}

[Serializable, NetSerializable]
public enum CryptominerState : byte
{
    /// <summary>
    /// Miner is turned off.
    /// </summary>
    Off,

    /// <summary>
    /// Miner is running normally.
    /// </summary>
    Normal,

    /// <summary>
    /// Miner is running but temperature is getting high.
    /// </summary>
    Warning,

    /// <summary>
    /// Miner is overheating, reduced efficiency.
    /// </summary>
    Overheat,

    /// <summary>
    /// Miner shut down due to critical temperature.
    /// </summary>
    Critical,

    /// <summary>
    /// Miner cannot operate due to low atmospheric pressure.
    /// </summary>
    NoAtmosphere,

    /// <summary>
    /// Miner cannot operate without disks inserted.
    /// </summary>
    NoDisks
}

[Serializable, NetSerializable]
public enum CryptominerVisuals : byte
{
    State,
    DiskCount
}

[Serializable, NetSerializable]
public enum CryptominerVisualLayers : byte
{
    Base,
    Lock,
    Level,
    Disks
}
