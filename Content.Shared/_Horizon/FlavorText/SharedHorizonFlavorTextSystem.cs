using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.FlavorText;

public abstract partial class SharedHorizonFlavorTextSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ErpStatusComponent, ExaminedEvent>(OnErpExamined);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<CharacterFactionMemberComponent, ExaminedEvent>(OnCharacterFactionExamined);
    }

    private void OnErpExamined(Entity<ErpStatusComponent> ent, ref ExaminedEvent args)
        => args.PushMarkup(Loc.GetString($"erp-status-{ent.Comp.Status}"), -5);

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId == null || !_proto.TryIndex<JobPrototype>(args.JobId, out var job))
            return;

        var factionComp = EnsureComp<CharacterFactionMemberComponent>(args.Mob);
        factionComp.Faction = job.ForceFaction ?? args.Profile.Faction;

        var oocComp = EnsureComp<OocDescriptionComponent>(args.Mob);
        oocComp.Description = args.Profile.OOCFlavorText;

        var erpComp = EnsureComp<ErpStatusComponent>(args.Mob);
        erpComp.Status = args.Profile.ErpStat;

        Dirty(args.Mob, factionComp);
        Dirty(args.Mob, oocComp);
        Dirty(args.Mob, erpComp);
    }

    private void OnCharacterFactionExamined(Entity<CharacterFactionMemberComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Faction == "None")
            return;

        var proto = _proto.Index(ent.Comp.Faction);
        args.PushMarkup(Loc.GetString($"character-faction-examine",
                                     ("ent", Identity.Name(ent.Owner, EntityManager)),
                                     ("faction", Loc.GetString(proto.Name)),
                                     ("color", proto.Color.ToHex())), 40);
    }

    public virtual void OpenFlavorMenu(EntityUid uid, EntityUid user, string description)
    {

    }
}
