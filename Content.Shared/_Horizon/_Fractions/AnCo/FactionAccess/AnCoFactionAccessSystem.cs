using Content.Shared._Horizon.FlavorText;
using Content.Shared.Access.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon._Fractions.AnCo.FactionAccess;

/// <summary>
/// System that handles faction-based access checks.
/// Blocks ActivatableUI opening and equipment if the user doesn't belong to an allowed faction.
/// Can be unlocked/locked by faction members using an ID card.
/// </summary>
public sealed class AnCoFactionAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoFactionAccessComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<AnCoFactionAccessComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<AnCoFactionAccessComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AnCoFactionAccessComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<AnCoFactionAccessComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<AnCoFactionAccessComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.CanToggleLock)
            return;

        var status = ent.Comp.Unlocked
            ? Loc.GetString("faction-access-examine-unlocked")
            : Loc.GetString("faction-access-examine-locked");

        args.PushMarkup(status);
    }

    private void OnShotAttempt(Entity<AnCoFactionAccessComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled)
            return;

        // Check if user is blacklisted - explode like Clumsy
        if (TryComp<CharacterFactionMemberComponent>(args.User, out var factionMember) &&
            ent.Comp.BlacklistedFactions.Contains(factionMember.Faction))
        {
            args.Cancel();
            DisableGun(ent, ent.Comp.BlacklistStunTime);

            // Only server handles all effects to prevent duplication
            if (!_net.IsServer)
                return;

            // Skip if already knocked down (prevents spam)
            if (HasComp<KnockedDownComponent>(args.User))
                return;

            if (ent.Comp.ExplodeOnBlacklist)
            {
                if (ent.Comp.BlacklistDamage != null)
                    _damageable.TryChangeDamage(args.User, ent.Comp.BlacklistDamage, origin: args.User);

                _stun.TryAddParalyzeDuration(args.User, ent.Comp.BlacklistStunTime);
                _audio.PlayPvs(ent.Comp.BlacklistSound, args.User);
                _popup.PopupEntity(Loc.GetString("faction-access-blacklist-explode"), ent, args.User);
            }
            else if (ent.Comp.DeniedMessage != null)
            {
                _popup.PopupEntity(Loc.GetString(ent.Comp.DeniedMessage), ent, args.User);
            }

            return;
        }

        if (!IsAllowed(args.User, ent))
        {
            args.Cancel();
            DisableGun(ent, ent.Comp.BlacklistStunTime);

            if (_net.IsServer && ent.Comp.DeniedMessage != null)
                _popup.PopupEntity(Loc.GetString(ent.Comp.DeniedMessage), ent, args.User);
        }
    }

    /// <summary>
    /// Temporarily disables the gun by setting NextFire to a future time.
    /// </summary>
    private void DisableGun(EntityUid gun, TimeSpan duration)
    {
        if (!TryComp<GunComponent>(gun, out var gunComp))
            return;

        var nextFire = _timing.CurTime + duration;
        if (gunComp.NextFire < nextFire)
        {
            gunComp.NextFire = nextFire;
            Dirty(gun, gunComp);
        }
    }

    private void OnInteractUsing(Entity<AnCoFactionAccessComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.CanToggleLock)
            return;

        // Check if using an ID card or PDA with ID card
        if (!HasComp<IdCardComponent>(args.Used) && !HasComp<PdaComponent>(args.Used))
            return;

        // Check if ID card has required access
        if (!HasUnlockAccess(args.Used, ent))
            return;

        // Toggle lock state
        ent.Comp.Unlocked = !ent.Comp.Unlocked;
        Dirty(ent);

        var message = ent.Comp.Unlocked
            ? Loc.GetString("faction-access-unlocked")
            : Loc.GetString("faction-access-locked");
        _popup.PopupClient(message, ent, args.User);

        args.Handled = true;
    }

    /// <summary>
    /// Checks if the ID card (or PDA with ID card) has the required access to toggle lock.
    /// </summary>
    private bool HasUnlockAccess(EntityUid used, Entity<AnCoFactionAccessComponent> target)
    {
        if (target.Comp.UnlockAccess == null)
            return false;

        // Get the ID card entity (from PDA if needed)
        EntityUid? idCard = null;

        if (TryComp<IdCardComponent>(used, out _))
        {
            idCard = used;
        }
        else if (TryComp<PdaComponent>(used, out var pda) && pda.ContainedId != null)
        {
            idCard = pda.ContainedId.Value;
        }

        if (idCard == null)
            return false;

        // Check if ID card has the required access
        if (!TryComp<AccessComponent>(idCard, out var access))
            return false;

        return access.Tags.Contains(target.Comp.UnlockAccess.Value);
    }

    private void OnUIOpenAttempt(Entity<AnCoFactionAccessComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!IsAllowed(args.User, ent))
        {
            args.Cancel();
            if (ent.Comp.DeniedMessage != null)
                _popup.PopupClient(Loc.GetString(ent.Comp.DeniedMessage), ent, args.User);
        }
    }

    private void OnEquipAttempt(Entity<AnCoFactionAccessComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!IsAllowed(args.Equipee, ent))
        {
            args.Cancel();
            if (ent.Comp.DeniedMessage != null)
                args.Reason = ent.Comp.DeniedMessage;
        }
    }

    /// <summary>
    /// Checks if a user is allowed to access an entity with AnCoFactionAccessComponent.
    /// </summary>
    public bool IsAllowed(EntityUid user, Entity<AnCoFactionAccessComponent> target)
    {
        if (!target.Comp.Enabled)
            return true;

        // Check blacklist first - blacklisted factions can NEVER access, even when unlocked
        if (TryComp<CharacterFactionMemberComponent>(user, out var factionMember))
        {
            if (target.Comp.BlacklistedFactions.Contains(factionMember.Faction))
                return false;
        }

        // If unlocked, everyone (except blacklisted) can access
        if (target.Comp.Unlocked)
            return true;

        // No restrictions if both lists are empty
        if (target.Comp.AllowedFactions.Count == 0 && target.Comp.DeniedFactions.Count == 0)
            return true;

        if (factionMember == null)
        {
            // No faction - only allow if no AllowedFactions specified
            return target.Comp.AllowedFactions.Count == 0;
        }

        var userFaction = factionMember.Faction;

        if (target.Comp.DeniedFactions.Contains(userFaction))
            return false;

        if (target.Comp.AllowedFactions.Count == 0)
            return true;

        return target.Comp.AllowedFactions.Contains(userFaction);
    }

    /// <summary>
    /// Checks if a user is allowed to access an entity with AnCoFactionAccessComponent.
    /// </summary>
    public bool IsAllowed(EntityUid user, EntityUid target)
    {
        if (!TryComp<AnCoFactionAccessComponent>(target, out var factionAccess))
            return true;

        return IsAllowed(user, (target, factionAccess));
    }
}
