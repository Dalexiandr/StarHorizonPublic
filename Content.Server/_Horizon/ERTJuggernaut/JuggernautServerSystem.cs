using Content.Shared.Actions;
using Content.Shared._Horizon.ERTJuggernaut;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server._Horizon.ERTJuggernaut;

public sealed class JuggernautServerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JuggernautComponent, ComponentStartup>(AddAction);
        SubscribeLocalEvent<JuggernautComponent, ComponentShutdown>(RemoveAction);
        SubscribeNetworkEvent<JuggernautChemMasterInjectEvent>(OnInject);
    }

    private void AddAction(EntityUid uid, JuggernautComponent component, ComponentStartup args)
    {
        _actionSystem.AddAction(uid, ref component.JuggernautEntity, component.JuggernautAction);
    }

    private void RemoveAction(EntityUid uid, JuggernautComponent component, ComponentShutdown args)
    {
        _actionSystem.RemoveAction(uid, component.JuggernautEntity);
    }

    private void OnInject(JuggernautChemMasterInjectEvent ev)
    {
        var uid = GetEntity(ev.Target);

        if (!TryComp<JuggernautComponent>(uid, out var component))
            return;

        foreach (var (reagent, amount) in ev.ReagentsToInject)
        {
            if (amount <= 0)
                continue;

            if (!component.AvailableReagents.TryGetValue(reagent, out var available))
                continue;

            var toTransfer = Math.Min(amount, available);

            if (toTransfer <= 0)
                continue;

            component.AvailableReagents[reagent] -= toTransfer;

            if (!TryComp(uid, out SolutionContainerManagerComponent? solutionContainerManager))
                return;

            if (!_solutionSystem.TryGetSolution((uid, solutionContainerManager), "chemicals", out var solution))
                return;

            _solutionSystem.TryAddReagent(solution.Value, reagent, toTransfer, out _);
        }

        Dirty(uid, component);
    }
}