using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Horizon.Expeditions;

[Prototype]
public sealed partial class ExpeditionGoalPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ExpeditionGoalPrototype>))]
    public string[]? Parents { get; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// Минимальное и максимальное число требующихся сущностей для цели
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public MinMax RandomAmount;

    /// <summary>
    /// Множитель для <see cref="RandomAmount"/>
    /// </summary>
    [DataField]
    public int AmountMultiplier = 1;

    /// <summary>
    /// Категория цели
    /// </summary>
    [DataField]
    public ProtoId<ExpeditionGoalCategoryPrototype> Specification = "Crew";

    /// <summary>
    /// Сущность, которая выдаётся вместе с наградой
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? RewardEntity;

    [DataField(required: true)]
    [NeverPushInheritance]
    public ExpeditionGoal Goal = default!;
}

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ExpeditionGoal
{
    /// <summary>
    /// Описание цели, отображаемое в UI
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Description = default!;

    /// <summary>
    /// Сущность для превью
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string IconEntity = default!;

    /// <summary>
    /// Основная награда за эту цель
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int Reward = default!;

    /// <summary>
    /// Название валюты, отображаемое в UI
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string CurrencyStr = "$";

    /// <summary>
    /// Тип валюты, в которой выдаётся награда
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string RequiredStack = "Credit";

    /// <summary>
    /// Сущность, которая выдаётся вместе с наградой
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? RewardEntity;

    /// <summary>
    /// Является ли задание контрабандным
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsContraband = false;

    /// <summary>
    /// Ивент, вызываемый при взятии таска
    /// </summary>
    [DataField(serverOnly: true), ViewVariables(VVAccess.ReadWrite)]
    public object? ClaimEvent;

    /// <summary>
    /// Необходимое количество для выполнения цели
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int RequiredAmount;

    /// <summary>
    /// Создаёт инстанцию <see cref="ExpeditionGoal"/> на основе данного
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public abstract ExpeditionGoal Instantiate(int amount);

    /// <summary>
    /// Проверяет, выполнится ли цель при продаже указанной сущности
    /// </summary>
    /// <param name="sellEntity"></param>
    /// <param name="entMan"></param>
    /// <returns>Соответствует ли требованиям цели сущность</returns>
    public abstract bool TryComplete(EntityUid sellEntity, IEntityManager entMan);
}

[Prototype]
public sealed partial class ExpeditionGoalCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;
}

