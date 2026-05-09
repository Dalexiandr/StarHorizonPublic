using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

/// <summary>
/// Цель, требующая определённое кол-во сущностей
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EntityExpeditionGoal : ExpeditionGoal
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string RequiredTag = default!;

    public override ExpeditionGoal Instantiate(int amount)
    {
        return new EntityExpeditionGoal()
        {
            Description = Loc.GetString(Description, ("amount", amount)),
            IconEntity = IconEntity,
            Reward = Reward,
            CurrencyStr = CurrencyStr,
            RequiredStack = RequiredStack,
            IsContraband = IsContraband,
            RequiredTag = RequiredTag,
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
        var tagSys = entMan.System<TagSystem>();

        if (tagSys.HasTag(sellEntity, RequiredTag))
            count += entMan.GetComponentOrNull<StackComponent>(sellEntity)?.Count ?? 1;
    }
}
