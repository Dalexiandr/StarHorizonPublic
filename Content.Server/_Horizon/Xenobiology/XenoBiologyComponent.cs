// Maded by Gorox. Discord - smeshinka112
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Horizon.Xenobiology;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class XenoBiologyComponent : Component
{
    /// Начальное количество очков для деления
    [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
    public int Points = 100;

    /// Сколько очков получает существо при атаке
    [DataField("pointsPerAttack"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerAttack = 10;

    /// Сколько очков необходимо для деления
    [DataField("pointsThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public int TargetToSplitPoints = 200;

    [DataField("pointLoss")]
    public int PointLoss = 100;

    /// Шанс мутации при делении
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MutationChance = 0.4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SplitChance = 0.2f;

    /// Прототип при удачной мутации
    [DataField("mutagen", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MutationEntity = "MobSlimesPet";

    /// Кем становится существо при делении, если имеет разум. Используйте прототип полиморфа
    [DataField("onMind", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PolymorphEntity = "RandomSlimePerson";

    public string? CurrentSpecies;
}
