using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Cytology;

public sealed class CytologySampleCombinatorSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedCytologyPetriDishSystem _cytologyPetriDish = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologySampleCombinatorComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CytologySampleCombinatorComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CytologySampleCombinatorComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CytologySampleCombinatorComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<CytologySampleCombinatorComponent, CytologySampleCombinatorUpdateProfileMessage>(OnUpdateProfile);
        SubscribeLocalEvent<CytologySampleCombinatorComponent, CytologySampleCombinatorUpdateDisksMessage>(OnUpdateDisks);
        SubscribeLocalEvent<CytologySampleCombinatorComponent, CytologySampleCombinatorDeleteSampleMessage>(OnDeleteSample);
    }

    private void OnComponentStartup(Entity<CytologySampleCombinatorComponent> ent, ref ComponentStartup args)
    {
        UpdateUiState(ent);
    }

    private void OnContainerChanged<T>(Entity<CytologySampleCombinatorComponent> ent, ref T ev)
    {
        UpdateUiState(ent);
    }

    private void OnUIOpened(Entity<CytologySampleCombinatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnUpdateProfile(Entity<CytologySampleCombinatorComponent> ent, ref CytologySampleCombinatorUpdateProfileMessage args)
    {
        var petriDishItem = _itemSlotsSystem.GetItemOrNull(ent.Owner, SharedCytologySampleCombinator.PetriDishSlotName);

        if (petriDishItem is not { } petriDish)
            return;

        if (!TryComp<CytologySampleContainerComponent>(petriDish, out var sampleContainer))
            return;

        if (args.SampleIndex < 0 || args.SampleIndex >= sampleContainer.CellSamples.Count)
            return;

        var cellSample = sampleContainer.CellSamples[args.SampleIndex];
        cellSample.StoredProfile = args.Profile;
        DirtyField(petriDish, sampleContainer, nameof(CytologySampleContainerComponent.CellSamples));

        UpdateUiState(ent);
    }

    private void OnUpdateDisks(Entity<CytologySampleCombinatorComponent> ent, ref CytologySampleCombinatorUpdateDisksMessage args)
    {
        var petriDishItem = _itemSlotsSystem.GetItemOrNull(ent.Owner, SharedCytologySampleCombinator.PetriDishSlotName);

        if (petriDishItem is not { } petriDish)
            return;

        if (!TryComp<CytologySampleContainerComponent>(petriDish, out var sampleContainer))
            return;

        if (args.SampleIndex < 0 || args.SampleIndex >= sampleContainer.CellSamples.Count)
            return;

        var cellSample = sampleContainer.CellSamples[args.SampleIndex];
        cellSample.SelectedDiskPrototypes = args.SelectedDiskPrototypes;
        DirtyField(petriDish, sampleContainer, nameof(CytologySampleContainerComponent.CellSamples));

        UpdateUiState(ent);
    }

    private void OnDeleteSample(Entity<CytologySampleCombinatorComponent> ent, ref CytologySampleCombinatorDeleteSampleMessage args)
    {
        var petriDishItem = _itemSlotsSystem.GetItemOrNull(ent.Owner, SharedCytologySampleCombinator.PetriDishSlotName);

        if (petriDishItem is not { } petriDish || !TryComp<CytologySampleContainerComponent>(petriDish, out var sampleContainer))
            return;

        if (args.SampleIndex < 0 || args.SampleIndex >= sampleContainer.CellSamples.Count)
            return;

        sampleContainer.CellSamples.RemoveAt(args.SampleIndex);

        DirtyField(petriDish, sampleContainer, nameof(CytologySampleContainerComponent.CellSamples));
        UpdateUiState(ent);

        _cytologyPetriDish.PetriDishUpdateAppearance(petriDish);
    }

    private void UpdateUiState(Entity<CytologySampleCombinatorComponent> ent)
    {
        var petriDish = _itemSlotsSystem.GetItemOrNull(ent.Owner, SharedCytologySampleCombinator.PetriDishSlotName);

        var state = BuildCellSamplesInfo(ent.Owner, petriDish);

        _userInterfaceSystem.SetUiState(ent.Owner, CytologySampleCombinatorUiKey.Key, state);
    }

    private CytologySampleCombinatorBoundUserInterfaceState BuildCellSamplesInfo(EntityUid console, EntityUid? petriDish)
    {
        if (petriDish is not { Valid: true }) //TODO это залупа
            return new CytologySampleCombinatorBoundUserInterfaceState(null, null, null);

        if (!TryComp<CytologySampleContainerComponent>(petriDish, out var sampleContainer))
            return new CytologySampleCombinatorBoundUserInterfaceState(null, null, null);

        List<CellSample> cellSampleInfos = new();
        List<String> cellNames = new();

        for (int i = 0; i < sampleContainer.CellSamples.Count; i++)
        {
            var cellSample = sampleContainer.CellSamples[i];

            cellSampleInfos.Add(cellSample);

            if (!_prototypeManager.TryIndex<CellSamplePrototype>(cellSample.ProtoID, out var cellSamplePrototype))
                continue;
            cellNames.Add(cellSamplePrototype.Name);
        }

        // Get available disks from console slots
        List<string> diskPrototypes = new();


        var disk1 = _itemSlotsSystem.GetItemOrNull(console, SharedCytologySampleCombinator.DiskSlot1Name);
        var disk2 = _itemSlotsSystem.GetItemOrNull(console, SharedCytologySampleCombinator.DiskSlot2Name);
        var disk3 = _itemSlotsSystem.GetItemOrNull(console, SharedCytologySampleCombinator.DiskSlot3Name);

        void AddDisk(EntityUid? disk)
        {
            if (disk == null)
                return;

            if (!TryComp<MetaDataComponent>(disk, out var meta))
                return;

            var protoId = meta.EntityPrototype?.ID;
            if (protoId == null)
                return;

            diskPrototypes.Add(protoId);
        }

        AddDisk(disk1);
        AddDisk(disk2);
        AddDisk(disk3);

        return new CytologySampleCombinatorBoundUserInterfaceState(cellSampleInfos, cellNames, diskPrototypes);
    }
}

