using System.Linq;
using Content.Server._Horizon.Botany.Components;
using Content.Server.Administration.Logs;
using Content.Server.Botany.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Server.Wires;
using Content.Shared._Horizon.Materials;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Mind;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Materials;

/// <summary>
/// Перенесённая из прошлого (16a0957a50d4ae1560235caeba03938dcbc8aa4c) старая система утилизации отходов
/// </summary>
public sealed class HorizonMaterialReclaimerSystem : SharedHorizonMaterialReclaimerSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedBodySystem _body = default!; //bobby
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<HorizonMaterialReclaimerComponent, InteractUsingEvent>(OnInteractUsing,
            before: [typeof(WiresSystem), typeof(SolutionTransferSystem)]);
        SubscribeLocalEvent<ActiveHorizonMaterialReclaimerComponent, PowerChangedEvent>(OnActivePowerChanged);
    }

    private void OnStartup(Entity<HorizonMaterialReclaimerComponent> entity, ref ComponentStartup args)
    {
        _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.SolutionContainerId);
    }

    private void OnPowerChanged(Entity<HorizonMaterialReclaimerComponent> entity, ref PowerChangedEvent args)
    {
        AmbientSound.SetAmbience(entity.Owner, entity.Comp.Enabled && args.Powered);
        entity.Comp.Powered = args.Powered;
        Dirty(entity);
    }

    private void OnInteractUsing(Entity<HorizonMaterialReclaimerComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // if we're trying to get a solution out of the reclaimer, don't destroy it
        if (_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.SolutionContainerId, out _, out var outputSolution) && outputSolution.Contents.Any())
        {
            if (TryComp<SolutionContainerManagerComponent>(args.Used, out var managerComponent) &&
                _solutionContainer.EnumerateSolutions((args.Used, managerComponent)).Any(s => s.Solution.Comp.Solution.AvailableVolume > 0))
            {
                if (_openable.IsClosed(args.Used))
                    return;

                if (TryComp<SolutionTransferComponent>(args.Used, out var transfer) &&
                    transfer.CanReceive)
                    return;
            }
        }

        args.Handled = TryStartProcessItem(entity.Owner, args.Used, entity.Comp, args.User);
    }

    private void OnActivePowerChanged(Entity<ActiveHorizonMaterialReclaimerComponent> entity, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            TryFinishProcessItem(entity, null, entity.Comp);
    }

    public override bool TryFinishProcessItem(EntityUid uid, HorizonMaterialReclaimerComponent? component = null, ActiveHorizonMaterialReclaimerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        if (!base.TryFinishProcessItem(uid, component, active))
            return false;

        if (active.ReclaimingContainer.ContainedEntities.FirstOrNull() is not { } item)
            return false;

        Container.Remove(item, active.ReclaimingContainer);
        Dirty(uid, component);

        // scales the output if the process was interrupted.
        var completion = 1f - Math.Clamp((float) Math.Round((active.EndTime - Timing.CurTime) / active.Duration),
            0f, 1f);
        Reclaim(uid, item, completion, component);

        return true;
    }

    public override void Reclaim(EntityUid uid,
        EntityUid item,
        float completion = 1f,
        HorizonMaterialReclaimerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.Reclaim(uid, item, completion, component);

        var xform = Transform(uid);

        SpawnMaterialsFromComposition(uid, item, completion * component.Efficiency, xform: xform);

        if (TryComp<TagComponent>(uid, out var tagReclaimer))
        {
            foreach (var tags in tagReclaimer.Tags)
            {
                if (tags == "Barrel")
                {
                    SpawnAlchoFromComposition(uid, item, completion, true, component, xform);
                    QueueDel(item);
                    return;
                }
            }
        }

        if (CanGib(uid, item, component))
        {
            _adminLogger.Add(LogType.Gib, LogImpact.Extreme, $"{ToPrettyString(item):victim} was gibbed by {ToPrettyString(uid):entity} ");
            SpawnChemicalsFromComposition(uid, item, completion, false, component, xform);
            _body.GibBody(item, true);
            _appearance.SetData(uid, RecyclerVisuals.Bloody, true);
        }
        else
        {
            SpawnChemicalsFromComposition(uid, item, completion, true, component, xform);
        }

        QueueDel(item);
    }

    private void SpawnMaterialsFromComposition(EntityUid reclaimer,
        EntityUid item,
        float efficiency,
        MaterialStorageComponent? storage = null,
        TransformComponent? xform = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(reclaimer, ref storage, ref xform, false))
            return;

        if (!Resolve(item, ref composition, false))
            return;

        foreach (var (material, amount) in composition.MaterialComposition)
        {
            var outputAmount = (int) (amount * efficiency);
            _materialStorage.TryChangeMaterialAmount(reclaimer, material, outputAmount, storage);
        }

        foreach (var (storedMaterial, storedAmount) in storage.Storage)
        {
            var stacks = _materialStorage.SpawnMultipleFromMaterial(storedAmount, storedMaterial,
                xform.Coordinates,
                out var materialOverflow);
            var amountConsumed = storedAmount - materialOverflow;
            _materialStorage.TryChangeMaterialAmount(reclaimer, storedMaterial, -amountConsumed, storage);
            foreach (var stack in stacks)
            {
                _stack.TryMergeToContacts(stack);
            }
        }
    }

    private void SpawnChemicalsFromComposition(EntityUid reclaimer,
        EntityUid item,
        float efficiency,
        bool sound = true,
        HorizonMaterialReclaimerComponent? reclaimerComponent = null,
        TransformComponent? xform = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(reclaimer, ref reclaimerComponent, ref xform))
            return;
        if (!_solutionContainer.TryGetSolution(reclaimer, reclaimerComponent.SolutionContainerId, out var outputSolution))
            return;

        efficiency *= reclaimerComponent.Efficiency;

        var totalChemicals = new Solution();

        if (Resolve(item, ref composition, false))
        {
            foreach (var (key, value) in composition.ChemicalComposition)
            {
                // TODO use ReagentQuantity
                totalChemicals.AddReagent(key, value * efficiency, false);
            }
        }

        // if the item we inserted has reagents, add it in.
        if (TryComp<SolutionContainerManagerComponent>(item, out var solutionContainer))
        {
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solutionContainer)))
            {
                var solution = soln.Comp.Solution;
                foreach (var quantity in solution.Contents)
                {
                    totalChemicals.AddReagent(quantity.Reagent.Prototype, quantity.Quantity * efficiency, false);
                }
            }
        }

        _solutionContainer.TryTransferSolution(outputSolution.Value, totalChemicals, totalChemicals.Volume);
        if (totalChemicals.Volume > 0)
        {
            _puddle.TrySpillAt(reclaimer, totalChemicals, out _, sound, transformComponent: xform);
        }
    }

    private void SpawnAlchoFromComposition(EntityUid reclaimer,
        EntityUid item,
        float efficiency,
        bool sound = true,
        HorizonMaterialReclaimerComponent? reclaimerComponent = null,
        TransformComponent? xform = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!TryComp<FermentationComponent>(item, out var fermentation))
            return;
        if (!Resolve(reclaimer, ref reclaimerComponent, ref xform))
            return;
        if (!_solutionContainer.TryGetSolution(reclaimer, reclaimerComponent.SolutionContainerId, out var outputSolution))
            return;


        var totalChemicals = new Solution();

        if (Resolve(item, ref composition, false))
        {
            foreach (var (key, value) in composition.ChemicalComposition)
            {
                // TODO use ReagentQuantity
                totalChemicals.AddReagent(fermentation.Fermentate, value * efficiency, false);
            }
        }

        // if the item we inserted has reagents, add it in.
        if (TryComp<SolutionContainerManagerComponent>(item, out var solutionContainer))
        {
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solutionContainer)))
            {
                var solution = soln.Comp.Solution;
                foreach (var quantity in solution.Contents)
                {
                    totalChemicals.AddReagent(fermentation.Fermentate, quantity.Quantity * efficiency, false);
                }
            }
        }

        _solutionContainer.TryTransferSolution(outputSolution.Value, totalChemicals, totalChemicals.Volume);
        if (totalChemicals.Volume > 0)
        {
            _puddle.TrySpillAt(reclaimer, totalChemicals, out _, sound, transformComponent: xform);
        }
    }
}
