using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Interaction.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Actions.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared._Horizon.CursedKatana;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Hands;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Horizon.CursedKatana;

public sealed class CursedKatanaSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CursedUserComponent, GetDemonMaskEvent>(OnGetMask);
        SubscribeLocalEvent<CursedUserComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CursedKatanaComponent, GotEquippedHandEvent>(OnKatanaEquipped);
        SubscribeLocalEvent<CursedUserComponent, ActivateCursedKatanaEvent>(OnActivateKatana);
        SubscribeLocalEvent<CursedUserComponent, DeactivateCursedKatanaEvent>(OnDeactivateKatana);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var katanaComp in EntityManager.EntityQuery<CursedKatanaComponent>(true))
        {
            if (!katanaComp.IsActive)
                return;

            katanaComp.DamageTimer -= frameTime;
            if (katanaComp.DamageTimer <= 0)
            {
                ApplyDamage(katanaComp);
                katanaComp.DamageTimer = katanaComp.DamageInterval;
            }
        }
    }

    public void ApplyDamage(CursedKatanaComponent katanaComp)
    {
        if (!EntityManager.TryGetComponent<DamageableComponent>(katanaComp.OwnerUid, out var damageableComp))
            return;

        var totalDamage = damageableComp.TotalDamage;
        var critThreshold = _mobThreshold.GetThresholdForState(katanaComp.OwnerUid, MobState.Critical);
        var currentHealth = critThreshold - totalDamage;

        if (currentHealth.Float() <= critThreshold.Float() * 0.2f)
            return;

        if (!_prototypeManager.TryIndex<DamageTypePrototype>("Slash", out var slashDamageType))
            return;

        var damage = new DamageSpecifier(slashDamageType, FixedPoint2.New(1.5));
        _damageable.TryChangeDamage(katanaComp.OwnerUid, damage);
    }

    private void OnKatanaEquipped(EntityUid uid, CursedKatanaComponent component, GotEquippedHandEvent args)
    {
        if (component.OwnerIdentified)
            return;

        if (!HasComp<CursedUserComponent>(args.User))
            SetOwner(uid, component, args.User);
    }

    private void SetOwner(EntityUid katanaUid, CursedKatanaComponent katanaComp, EntityUid ownerUid)
    {
        AddComp<CursedUserComponent>(ownerUid);

        if (TryComp<CursedUserComponent>(ownerUid, out var ownerComp))
        {
            ownerComp.OwnerUid = ownerUid;
            ownerComp.KatanaUid = katanaUid;
            katanaComp.OwnerUid = ownerUid;
            katanaComp.OwnerIdentified = true;

            _popupSystem.PopupCursor(Loc.GetString("Непонятные символы оказываются на проклятой катане..."), ownerUid, PopupType.Large);

            AddComp<PointLightComponent>(katanaUid);
            TryComp<PointLightComponent>(katanaUid, out var light);
            _pointLight.SetColor(katanaUid, Color.Red, light);
            _pointLight.SetRadius(katanaUid, (float)2.0, light);
            _pointLight.SetEnergy(katanaUid, (float)1.0, light);

            var message = _random.Pick(katanaComp.OneBlockMessage);
            _chat.TrySendInGameICMessage(ownerUid, message, InGameICChatType.Speak, true);

            _actionSystem.AddAction(ownerUid, ref ownerComp.GetDemonMaskActionEntity, ownerComp.GetDemonMaskAction);

            if (TryComp<CursedKatanaComponent>(katanaUid, out var comp))
            {
                _actionSystem.AddAction(ownerUid, ref comp.ActivateCursedKatanaActionEntity, comp.ActivateCursedKatanaAction);
            }
        }
    }

    private void OnActivateKatana(EntityUid uid, CursedUserComponent component, ActivateCursedKatanaEvent args)
    {
        if (component.KatanaUid == null)
            return;

        var katanaUid = component.KatanaUid.Value;

        if (TryComp<CursedKatanaComponent>(katanaUid, out var katanaComp))
            ActivateKatana(katanaUid, katanaComp, uid);
    }

    private void OnDeactivateKatana(EntityUid uid, CursedUserComponent component, DeactivateCursedKatanaEvent args)
    {
        if (component.KatanaUid == null)
            return;

        var katanaUid = component.KatanaUid.Value;

        if (TryComp<CursedKatanaComponent>(katanaUid, out var katanaComp))
            DeActivateKatana(katanaUid, katanaComp, uid);
    }

    private void ActivateKatana(EntityUid katanaUid, CursedKatanaComponent katanaComp, EntityUid ownerUid)
    {
        katanaComp.IsActive = true;

        _popupSystem.PopupCursor(Loc.GetString("КРУШИ! РУБИ! УБИВАЙ!"), ownerUid, PopupType.Large);

        if (TryComp<MovementSpeedModifierComponent>(ownerUid, out var moveComp))
        {
            katanaComp.OriginalWalkSpeed = moveComp.BaseWalkSpeed;
            katanaComp.OriginalSprintSpeed = moveComp.BaseSprintSpeed;

            var newWalkSpeed = katanaComp.OriginalWalkSpeed * 1.3f; //+ 30%
            var newSprintSpeed = katanaComp.OriginalSprintSpeed * 1.3f; //+ 30%

            _movementSpeedModifierSystem.ChangeBaseSpeed(ownerUid, newWalkSpeed, newSprintSpeed, moveComp.Acceleration);
        }

        if (TryComp<ReflectComponent>(katanaUid, out var reflectComponent))
        {
            reflectComponent.ReflectProb = 0.7f;
        }

        if (TryComp<MeleeWeaponComponent>(katanaUid, out var meleeComp))
        {
            katanaComp.OriginalDamage = meleeComp.Damage;

            meleeComp.AttackRate = 3;
            meleeComp.Damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), FixedPoint2.New(15));
        }

        AddComp<UnremoveableComponent>(katanaUid);

        TryComp<PointLightComponent>(katanaUid, out var light);
        _pointLight.SetColor(katanaUid, Color.DarkRed, light);
        _pointLight.SetRadius(katanaUid, (float)10.0, light);
        _pointLight.SetEnergy(katanaUid, (float)4.0, light);

        var message = _random.Pick(katanaComp.TwoBlockMessage);
        _chat.TrySendInGameICMessage(ownerUid, message, InGameICChatType.Speak, true);

        _actionSystem.RemoveAction(ownerUid, katanaComp.ActivateCursedKatanaActionEntity);
        _actionSystem.AddAction(ownerUid, ref katanaComp.DeactivateCursedKatanaActionEntity, katanaComp.DeactivateCursedKatanaAction);
    }

    private void DeActivateKatana(EntityUid katanaUid, CursedKatanaComponent katanaComp, EntityUid ownerUid)
    {
        katanaComp.IsActive = false;

        _popupSystem.PopupCursor(Loc.GetString("Вы чувствуете что сила полученная вам, угасает... Кажется всё закончилось."), ownerUid, PopupType.Large);

        if (TryComp<MovementSpeedModifierComponent>(ownerUid, out var moveMod))
        {
            _movementSpeedModifierSystem.ChangeBaseSpeed(ownerUid, katanaComp.OriginalWalkSpeed, katanaComp.OriginalSprintSpeed, moveMod.Acceleration);
        }

        if (TryComp<MeleeWeaponComponent>(katanaUid, out var meleeComp))
        {
            meleeComp.AttackRate = 1;
            meleeComp.Damage = katanaComp.OriginalDamage;
        }

        if (TryComp<ReflectComponent>(katanaUid, out var reflectComponent))
        {
            reflectComponent.ReflectProb = 0.1f;
        }

        RemComp<UnremoveableComponent>(katanaUid);

        TryComp<PointLightComponent>(katanaUid, out var light);
        _pointLight.SetColor(katanaUid, Color.Red, light);
        _pointLight.SetRadius(katanaUid, (float)2.0, light);
        _pointLight.SetEnergy(katanaUid, (float)1.0, light);

        var message = _random.Pick(katanaComp.ThreeBlockMessage);
        _chat.TrySendInGameICMessage(ownerUid, message, InGameICChatType.Speak, true);

        _actionSystem.RemoveAction(ownerUid, katanaComp.DeactivateCursedKatanaActionEntity);
        _actionSystem.AddAction(ownerUid, ref katanaComp.ActivateCursedKatanaActionEntity, katanaComp.ActivateCursedKatanaAction);
    }

    private void OnGetMask(EntityUid ownerUid, CursedUserComponent ownerComp, GetDemonMaskEvent args)
    {
        var user = args.Performer;
        var mask = Spawn(ownerComp.DemonMaskPrototype, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, mask);

        if (TryComp<DemonMaskComponent>(mask, out var maskComp))
        {
            maskComp.OwnerUid = user;
            maskComp.OwnerIdentified = true;
            ownerComp.MaskUid = mask;
            _popupSystem.PopupCursor("Вы стали обладателем могущественной маски...", user, PopupType.Large);
        }

        _actionSystem.RemoveAction(user, ownerComp.GetDemonMaskActionEntity);
    }

    private void OnMobStateChanged(EntityUid ownerUid, CursedUserComponent ownerComp, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical && ownerComp.KatanaUid.HasValue)
        {
            var katanaUid = ownerComp.KatanaUid.Value;
            if (TryComp<CursedKatanaComponent>(katanaUid, out var katanaComp) && katanaComp.IsActive)
            {
                DeActivateKatana(katanaUid, katanaComp, ownerUid);
            }
        }
    }
}
