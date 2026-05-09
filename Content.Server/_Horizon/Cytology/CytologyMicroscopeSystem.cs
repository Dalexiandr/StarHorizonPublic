using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server._Horizon.Cytology.Components;


namespace Content.Server._Horizon.Cytology;

public sealed class CytologyMicroscopeSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyMicroscopeComponent, ComponentStartup>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
    }

    private void SubscribeUpdateUiState<T>(Entity<CytologyMicroscopeComponent> ent, ref T ev)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<CytologyMicroscopeComponent> ent)
    {
        var inputContainer = _itemSlotsSystem.GetItemOrNull(ent.Owner, SharedCytologyMicroscope.InputSlotName);

        var state = new MicroscopeBoundUserInterfaceState(BuildInputContainerInfo(inputContainer));

        _userInterfaceSystem.SetUiState(ent.Owner, MicroscopeUiKey.Key, state);
    }

    private List<CellSampleInfo>? BuildInputContainerInfo(EntityUid? container)
    {
        if (container is not { Valid: true })
            return null;

        if (!TryComp<CytologySampleContainerComponent>(container, out var petriDishSampleContainerComp))
            return null;

        List<CellSampleInfo> cellSampleInfos = new();

        foreach (var cellSample in petriDishSampleContainerComp.CellSamples)
        {
            if (!_prototypeManager.TryIndex<CellSamplePrototype>(cellSample.ProtoID, out var cellSamplePrototype))
                continue;

            cellSampleInfos.Add(BuildPetriDishInfo(cellSamplePrototype));
        }

        return cellSampleInfos;
    }

    private static CellSampleInfo BuildPetriDishInfo(CellSamplePrototype cellSamplePrototype)
    {
        return new CellSampleInfo(cellSamplePrototype.Name, cellSamplePrototype.RequiredChemicals, cellSamplePrototype.SupplementaryChemicals.Keys.ToList(),
                                  cellSamplePrototype.SuppressiveChemicals.Keys.ToList(), cellSamplePrototype.GrowthRateInSeconds, cellSamplePrototype.ViralSusceptibility);
    }
}
