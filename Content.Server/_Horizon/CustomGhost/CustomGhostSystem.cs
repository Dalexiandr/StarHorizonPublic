using Content.Server.GameTicking;
using Content.Shared._Horizon.CustomGhost;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.CustomGhost;

public sealed class CustomGhostSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private Dictionary<string, string> _ghostMappings = new();

    public override void Initialize()
    {
        base.Initialize();
        LoadGhostMappings();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<CustomGhostMappingPrototype>())
        {
            LoadGhostMappings();
        }
    }

    private void LoadGhostMappings()
    {
        _ghostMappings.Clear();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CustomGhostMappingPrototype>())
        {
            foreach (var (ckey, ghostPrototype) in prototype.Mappings)
            {
                _ghostMappings[ckey] = ghostPrototype;
            }
        }
    }
    public string GetGhostPrototypeForPlayer(string ckey)
    {
        return _ghostMappings.TryGetValue(ckey, out var prototype)
            ? prototype
            : GameTicker.AdminObserverPrototypeName;
    }
}
