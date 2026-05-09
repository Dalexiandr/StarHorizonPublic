using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Power;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedCytologyGrowingVatSystem : EntitySystem
{

    public const string BeakerSlotName = "beakerSlot";
    public const string PetriDishSlotName = "petriDishSlot";


    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedCytologyPetriDishSystem _petriDishSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CytologyGrowingVatComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CytologyGrowingVatComponent, EntInsertedIntoContainerMessage>(OnSolutionContainerChanged);
        SubscribeLocalEvent<CytologyGrowingVatComponent, EntRemovedFromContainerMessage>(OnSolutionContainerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CytologyGrowingVatComponent>();
        while (query.MoveNext(out var uid, out var cytologyGrowingVatComp))
        {

            if (!cytologyGrowingVatComp.IsActive)
                continue;

            Entity<CytologyGrowingVatComponent> growingVat = (uid, cytologyGrowingVatComp);

            if (growingVat.Comp.NextUpdate > _timing.CurTime)
                continue;

            growingVat.Comp.NextUpdate = _timing.CurTime + growingVat.Comp.UpdateInterval;

            var dishEnt = _itemSlotsSystem.GetItemOrNull(uid, PetriDishSlotName); //Get an inserted petri dish

            Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.IsError, growingVat.Comp.StopWithError);
            Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.WithFoam, growingVat.Comp.WithFoam);
            growingVat.Comp.WithFoam = false; //Set these flags in advance. If they don't update, it means that the code is interrupted
            growingVat.Comp.StopWithError = true;

            if (dishEnt is not { } petriDishUid)
                continue;

            if (!cytologyGrowingVatComp.IsPowered)
                continue;

            ProcessGrowth(growingVat, petriDishUid);

        }
    }

    private void OnPowerChanged(Entity<CytologyGrowingVatComponent> growingVat, ref PowerChangedEvent args)
    {
        growingVat.Comp.IsPowered = args.Powered;
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.Powered, args.Powered);
    }

    public bool TryGetSolutionFromBeaker(EntityUid uid, [NotNullWhen(true)] out Solution? solution, [NotNullWhen(true)] out Entity<SolutionComponent> solutionEntity)
    {
        solutionEntity = default;
        solution = default;

        var beakerEnt = _itemSlotsSystem.GetItemOrNull(uid, BeakerSlotName);

        if (beakerEnt is not { } beakerEntUid)
            return false;

        if (!_solutionContainerSystem.TryGetFitsInDispenser(beakerEntUid, out var solutionComp, out solution))
            return false;

        solutionEntity = solutionComp.Value;

        return true;
    }

    private void OnSolutionContainerChanged<T>(Entity<CytologyGrowingVatComponent> growingVat, ref T ev)
    {
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.WithFoam, false);
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.WithLiquid, TryGetSolutionFromBeaker(growingVat.Owner, out _, out _));
    }

    private void ProcessGrowth(Entity<CytologyGrowingVatComponent> growingVat, EntityUid petriDish)
    {

        if (!TryComp<CytologySampleContainerComponent>(petriDish, out var petriDishSampleContainerComp))
            return;

        var beakerEnt = _itemSlotsSystem.GetItemOrNull(growingVat.Owner, BeakerSlotName); //Get an inserted beaker

        if (!TryGetSolutionFromBeaker(growingVat.Owner, out var beakerSolution, out var solutionEntity))
            return;

        var cellSamples = petriDishSampleContainerComp.CellSamples;
        if (cellSamples.Count == 0)
            return;

        var reagentLookup = new Dictionary<string, FixedPoint2>();
        foreach (var rq in beakerSolution.Contents) //Get all the reagents in the beaker. I haven't found a function. that does this
        {
            var id = rq.Reagent.Prototype;
            if (!reagentLookup.TryAdd(id, rq.Quantity))
                reagentLookup[id] += rq.Quantity;
        }

        for (var i = cellSamples.Count - 1; i >= 0; i--) //Go through each cell
        {
            var cell = cellSamples[i];
            if (!_prototypeManager.TryIndex<CellSamplePrototype>(cell.ProtoID, out var proto))
                continue;

            var hasAllRequired = true;
            foreach (var required in proto.RequiredChemicals)
            {
                if (!reagentLookup.TryGetValue(required, out var qty) || qty <= FixedPoint2.Zero)
                {
                    hasAllRequired = false; //If any of the required reagents is missing
                    break;
                }
            }

            if (!hasAllRequired)
                continue;

            SetCellGrowProgress(proto, cell, reagentLookup);

            ConsumeChemicals(solutionEntity, proto.RequiredChemicals);
            ConsumeChemicals(solutionEntity, proto.SupplementaryChemicals.Keys);
            ConsumeChemicals(solutionEntity, proto.SuppressiveChemicals.Keys);

            growingVat.Comp.StopWithError = false; //Those are the flags. If the code reaches here, then everything is fine
            growingVat.Comp.WithFoam = true;

            if (cell.GrowProgress >= 1f && proto.SpawnMobByPrototype != null)
            {

                PredictedSpawnAtPosition(growingVat.Comp.SmokePrototype, Transform(petriDish).Coordinates.SnapToGrid(EntityManager, _mapManager));

                foreach (var mob in proto.SpawnMobByPrototype)
                {
                    var spawnedMob = PredictedSpawnAtPosition(mob, Transform(petriDish).Coordinates);
                    TrySetHumanoidApperenceToMobFromCellSampleInfo(spawnedMob, cell);
                }
                cellSamples.RemoveAt(i);

                _petriDishSystem.PetriDishUpdateAppearance(petriDish);
            }
        }


    }

    private void TrySetHumanoidApperenceToMobFromCellSampleInfo(EntityUid spawnedMob, CellSample cell)
    {
        if (cell.StoredProfile is not { } profile)
            return;

        _humanoidSystem.LoadProfile(spawnedMob, profile);
        _metaDataSystem.SetEntityName(spawnedMob, profile.Name);

        // Apply disk modifications if any disks are selected // TODO вынести отдельно
        if (cell.SelectedDiskPrototypes != null && cell.SelectedDiskPrototypes.Count > 0)
        {
            var diskSystem = EntityManager.System<CytologyDiskSystem>();
            diskSystem.ApplyDiskModifications(spawnedMob, cell.SelectedDiskPrototypes);
        }
    }

    private void SetCellGrowProgress(CellSamplePrototype proto, CellSample cell, Dictionary<string, FixedPoint2> reagentLookup)
    {
        var modifier = 1f;

        modifier = ApplyModifiers(modifier, proto.SupplementaryChemicals, reagentLookup);
        modifier = ApplyModifiers(modifier, proto.SupplementaryChemicals, reagentLookup);

        if (modifier < 0.1f)
            modifier = 0.1f;

        var basePerSecond = 1f / MathF.Max(0.001f, proto.GrowthRateInSeconds);
        cell.GrowProgress += basePerSecond * modifier;
    }

    private float ApplyModifiers(float modifier, Dictionary<ProtoId<ReagentPrototype>, float> chemicals, Dictionary<string, FixedPoint2> reagentLookup)
    {
        foreach (var (chem, mult) in chemicals)
        {
            if (reagentLookup.TryGetValue(chem, out var qty) && qty > FixedPoint2.Zero)
                modifier += mult;
        }

        return modifier;
    }

    private void ConsumeChemicals(Entity<SolutionComponent> beakerEnt, IEnumerable<ProtoId<ReagentPrototype>> chemicals)
    {
        foreach (var chem in chemicals)
        {
            _solutionContainerSystem.RemoveReagent(beakerEnt, chem, FixedPoint2.New(1));
        }
    }

    private void OnMapInit(Entity<CytologyGrowingVatComponent> growingVat, ref MapInitEvent args)
    {
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualLayers.Indicator, false);
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualLayers.Liquid, false);
    }
}
