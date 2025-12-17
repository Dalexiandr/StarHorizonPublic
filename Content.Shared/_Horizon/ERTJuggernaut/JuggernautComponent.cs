using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Horizon.ERTJuggernaut;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JuggernautComponent : Component
{
    [DataField("juggernautAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string JuggernautAction = "JuggernautSettings";

    [DataField("juggernautEntity")]
    [AutoNetworkedField]
    public EntityUid? JuggernautEntity;

    [DataField("maxReagentAmount")]
    public float MaxReagentAmount = 30f;

    [DataField("selectedAmounts")]
    [AutoNetworkedField]
    public Dictionary<string, float> SelectedAmounts = new();

    public readonly Dictionary<string, string> ReagentNames = new()
    {
        { "Bicaridine", "Бикаридин" },
        { "DexalinPlus", "Дексалин +" },
        { "Diphenhydramine", "Дифенгидрамин" },
        { "Pyrazine", "Пиразин" },
        { "Insuzine", "Инсузин" }
    };

    public readonly Dictionary<string, string> ReagentDescriptions = new()
    {
        { "Bicaridine", "Анальгетик, который очень эффективен при лечении механических повреждений." },
        { "DexalinPlus", "Используется для лечения кислородного голодания и потери крови." },
        { "Diphenhydramine", "Снижает нервозность и дрожь, лечит повреждения ядами." },
        { "Pyrazine", "Эффективно лечит ожоги, полученные в самых жарких пожарах. При передозировке вызывает обширное внутреннее кровотечение." },
        { "Insuzine", "Быстро восстанавливает ткани, омертвевшие в результате поражения электрическим током, но при этом слегка охлаждает. Полностью замораживает пациента при передозировке." }
    };

    [DataField("availableReagents")]
    [AutoNetworkedField]
    public Dictionary<string, float> AvailableReagents = new()
    {
        { "Bicaridine", 30f },
        { "DexalinPlus", 30f },
        { "Diphenhydramine", 30f },
        { "Pyrazine", 10f },
        { "Insuzine", 10f }
    };
}

[Serializable, NetSerializable]
public sealed class JuggernautChemMasterInjectEvent : EntityEventArgs
{
    public NetEntity Target;
    public Dictionary<string, float> ReagentsToInject;

    public JuggernautChemMasterInjectEvent(NetEntity target, Dictionary<string, float> reagentsToInject)
    {
        Target = target;
        ReagentsToInject = reagentsToInject;
    }
}
