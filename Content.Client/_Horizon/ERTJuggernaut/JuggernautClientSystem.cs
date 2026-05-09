using Content.Shared._Horizon.ERTJuggernaut;

namespace Content.Client._Horizon.ERTJuggernaut;

public sealed class JuggernautClientSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void SendInjectEvent(EntityUid target, Dictionary<string, float> selectedReagents)
    {
        var netEntity = _entityManager.GetNetEntity(target);
        RaiseNetworkEvent(new JuggernautChemMasterInjectEvent(netEntity, selectedReagents));
    }
}