using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Expeditions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExpeditionGoalsConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<ExpeditionGoalCategoryPrototype>> Categories = new()
    {
        "Crew",
        "Science",
        "Engineer",
        "Expeditionary",
        "Security",
        "Medical",
    };
}
