using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Medical.Limbs;

public interface IImplantable;
public partial interface IWithAction : IImplantable
{
    public bool EntityIcon { get; } // It shouldn't be here, but I’m too lazy to redo everything.

    public EntProtoId Action { get; }

    public EntityUid? ActionEntity { get; set; }
}
