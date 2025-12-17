using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedCytologyMicroscope
{
    public const string InputSlotName = "petriDishSlot";
}

[Serializable, NetSerializable]
public sealed class CellSampleInfo
{

    public readonly string DisplayName;

    public readonly List<ProtoId<ReagentPrototype>> RequiredChemicals;

    public readonly List<ProtoId<ReagentPrototype>> SupplementaryChemicals;

    public readonly List<ProtoId<ReagentPrototype>> SuppressiveChemicals;

    public readonly float GrowthRateInSeconds;

    public readonly float ViralSusceptibility;

    public CellSampleInfo(string displayName,
                          List<ProtoId<ReagentPrototype>> requiredChemicals,
                          List<ProtoId<ReagentPrototype>> supplementaryChemicals,
                          List<ProtoId<ReagentPrototype>> suppressiveChemicals,
                          float growthRateInSeconds, float viralSusceptibility)
    {
        DisplayName = displayName;
        RequiredChemicals = requiredChemicals;
        SupplementaryChemicals = supplementaryChemicals;
        SuppressiveChemicals = suppressiveChemicals;
        GrowthRateInSeconds = growthRateInSeconds;
        ViralSusceptibility = viralSusceptibility;
    }
}

[Serializable, NetSerializable]
public sealed class MicroscopeBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<CellSampleInfo>? CellSampleInfo;

    public MicroscopeBoundUserInterfaceState(List<CellSampleInfo>? cellSampleInfo)
    {
        CellSampleInfo = cellSampleInfo;
    }
}

[Serializable, NetSerializable]
public enum MicroscopeUiKey
{
    Key
}
