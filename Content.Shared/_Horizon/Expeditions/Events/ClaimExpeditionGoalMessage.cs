using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class ClaimExpeditionGoalMessage : BoundUserInterfaceMessage
{
    public int OptionId;
    public ProtoId<ExpeditionGoalCategoryPrototype> Specification;

    public ClaimExpeditionGoalMessage(int optionId, ProtoId<ExpeditionGoalCategoryPrototype> specification)
    {
        OptionId = optionId;
        Specification = specification;
    }
}
