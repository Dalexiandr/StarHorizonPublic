using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

/// <summary>
/// Цель, требующая определённое число реагента
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ReagentExpeditionGoal : ExpeditionGoal
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string RequiredReagent = default!;

    public override ExpeditionGoal Instantiate(int amount)
    {
        return new ReagentExpeditionGoal()
        {
            Description = Loc.GetString(Description, ("amount", amount)),
            IconEntity = IconEntity,
            Reward = Reward,
            CurrencyStr = CurrencyStr,
            RequiredStack = RequiredStack,
            IsContraband = IsContraband,
            RequiredReagent = RequiredReagent,
            RequiredAmount = amount,
            ClaimEvent = ClaimEvent
        };
    }

    public override bool TryComplete(EntityUid sellEntity, IEntityManager entMan)
    {
        int count = 0;

        IncreaseFromStack(sellEntity, ref count, entMan);

        var entStorage = entMan.System<SharedEntityStorageSystem>();
        SharedEntityStorageComponent? storage = null;
        entStorage.ResolveStorage(sellEntity, ref storage);

        if (storage != null)
        {
            foreach (var item in storage.Contents.ContainedEntities)
                IncreaseFromStack(item, ref count, entMan);
        }

        return count >= RequiredAmount;
    }

    private void IncreaseFromStack(EntityUid sellEntity, ref int count, IEntityManager entMan)
    {
        var solutionContainer = entMan.System<SharedSolutionContainerSystem>();

        if (!entMan.TryGetComponent<SolutionContainerManagerComponent>(sellEntity, out var solMan))
            return;

        foreach (var item in solMan.Containers)
        {
            if (!solutionContainer.TryGetSolution(solMan, item, out var solution))
                continue;

            foreach (var reagent in solution.Contents)
            {
                if (reagent.Reagent.Prototype == RequiredReagent)
                    count += reagent.Quantity.Int();
            }
        }
    }
}
