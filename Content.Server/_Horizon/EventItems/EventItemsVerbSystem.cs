using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.EventItems;

/// <summary>
/// Registers "Grant as Event Item" debug verb on entities.
/// </summary>
public sealed class EventItemsVerbSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("event-items-verb");

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Debug))
            return;

        // Only show for entities that have a prototype (actual items)
        var meta = MetaData(args.Target);
        if (meta.EntityPrototype == null)
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("event-item-verb-grant"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Act = () =>
            {
                _sawmill.Info($"Admin {player.Name} opened Grant Event Item EUI for entity {ToPrettyString(args.Target)} (proto: {meta.EntityPrototype?.ID}).");
                _euiManager.OpenEui(
                    new GrantEventItemEui(GetNetEntity(args.Target), args.Target),
                    player);
            },
            Impact = LogImpact.High,
        };
        args.Verbs.Add(verb);
    }
}
