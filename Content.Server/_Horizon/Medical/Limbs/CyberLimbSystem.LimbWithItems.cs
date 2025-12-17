using System.Linq;
using Content.Shared._Horizon.Medical.Limbs;
using Content.Shared.Armor;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Medical.Limbs;

public sealed partial class CyberLimbSystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _moveSystem = null!;
    [Dependency] private readonly SharedArmorSystem _armorSystem = null!;

    private readonly Dictionary<EntityUid, (BaseSpeed, List<float>)> _speedCoefficients = new();
    private readonly Dictionary<EntityUid, List<DamageModifierSet>> _armorCoefficients = new();

    private readonly Dictionary<string, float> _armorBlockers = new()
    {
        {"Asphyxiation", 1f},
        {"Bloodloss", 0.5f},
        {"Blunt", 0.75f},
        {"Cellular", 1f},
        {"Caustic", 0.75f},
        {"Cold", 0.5f},
        {"Heat", 0.75f},
        {"Piercing", 0.75f},
        {"Poison", 0.5f},
        {"Shock", 0.75f},
        {"Slash", 0.75f},
        {"Structural", 1f},
        {"Holy", 1f},
    };

private void InitializeLimbWithItems()
    {
        base.Initialize();
        SubscribeLocalEvent<LimbWithItemsComponent, ComponentInit>(OnLimbWithItemsInit);
        SubscribeLocalEvent<LimbWithItemsComponent, ToggleLimbEvent>(OnLimbToggle);
    }

    private void OnLimbToggle(Entity<LimbWithItemsComponent> ent, ref ToggleLimbEvent args)
    {
        ent.Comp.Toggled = !ent.Comp.Toggled;

        if (ent.Comp.Toggled)
        {
            foreach (var item in ent.Comp.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                var hands = EnsureComp<HandsComponent>(args.Performer);
                _hands.AddHand(args.Performer, handId, HandLocation.Functional, hands);
                _hands.DoPickup(args.Performer, hands.Hands[handId], item, hands);
                EnsureComp<UnremoveableComponent>(item);
            }
        }
        else
        {
            var container = _container.EnsureContainer<Container>(ent.Owner, "cyberlimb", out _);
            foreach (var item in ent.Comp.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                RemComp<UnremoveableComponent>(item);
                var hands = EnsureComp<HandsComponent>(args.Performer);
                _container.Insert(_slEnt.Entity<TransformComponent, MetaDataComponent, PhysicsComponent>(item), container, force: true);
                _hands.RemoveHand(args.Performer, handId, hands);
            }
        }

        if (_slEnt.TryEntity<BaseLayerIdComponent, BaseLayerIdToggledComponent, BodyPartComponent>(ent.Owner, out var limb, false)
            && _slEnt.TryEntity<HumanoidAppearanceComponent>(args.Performer, out var performer, false))
            _limb.ToggleLimbVisual(performer.Value, limb.Value, ent.Comp.Toggled);

        _audio.PlayPvs(ent.Comp.Sound, args.Performer);

        Dirty(ent);
    }

    private void OnLimbWithItemsInit(Entity<LimbWithItemsComponent> limb, ref ComponentInit args)
    {
        if (limb.Comp.ItemEntities.Count == limb.Comp.Items.Count)
            return;
        var container = _container.EnsureContainer<Container>(limb.Owner, "cyberlimb", out _);

        limb.Comp.ItemEntities = [.. limb.Comp.Items.Select(EnsureItem)];

        DirtyEntity(limb);
        return;

        EntityUid EnsureItem(EntProtoId proto)
        {
            var id = Spawn(proto);
            _container.Insert(_slEnt.Entity<TransformComponent, MetaDataComponent, PhysicsComponent>(id), container, force: true);
            return id;
        }
    }

// _HORIZON STARTS

    /// <summary>
    /// Метод предназначенный для изменения скорости объекта
    /// </summary>
    /// <param name="target"> Объект к которому применяются изменения </param>
    /// <param name="modifier"> Значение в процентах для увеличения скорости </param>
    /// <param name="remove"> Стоит ли убавлять скорость вместо его увеличения </param>
    public void IncreaseSpeed(EntityUid target, float modifier, bool remove = false)
    {
        if (!TryComp<MovementSpeedModifierComponent>(target, out var move))
            return;

        List<float> modifiers;
        if (_speedCoefficients.TryGetValue(target, out var oldModifiers))
        {
            modifiers = oldModifiers.Item2;
            if (remove)
                modifiers.Remove(modifier);
            else
                modifiers.Add(modifier);

            if (modifiers.Count == 0)
            {
                _moveSystem.ChangeBaseSpeed(target,
                    oldModifiers.Item1.WalkSpeed,
                    oldModifiers.Item1.SprintSpeed,
                    move.Acceleration);
                return;
            }

            _speedCoefficients[target] = (oldModifiers.Item1, modifiers);
        }
        else
        {
            modifiers = [modifier];
            var baseSpeed = new BaseSpeed(move.BaseWalkSpeed, move.BaseSprintSpeed);
            oldModifiers = _speedCoefficients[target] = (baseSpeed, modifiers);
        }

        var totalCoefficient = 1 + modifiers.Sum();
        var walkSpeed = oldModifiers.Item1.WalkSpeed * totalCoefficient;
        var sprintSpeed = oldModifiers.Item1.SprintSpeed * totalCoefficient;

        _moveSystem.ChangeBaseSpeed(target, walkSpeed, sprintSpeed, move.Acceleration);
    }

    /// <summary>
    /// Метод предназначенный для изменения значений брони у объекта
    /// </summary>
    /// <param name="target"> Объект к которому мы применяем изменения </param>
    /// <param name="resistance"> Список изменяемых параметров, если они не были изменены до этого присваиваются как основные </param>
    /// <param name="remove"> Если true значения будут убавляться вместо прибавления </param>
    public void IncreaseArmor(EntityUid target, DamageModifierSet resistance, bool remove = false)
    {
        var totalResistance = new DamageModifierSet();
        if (TryComp<ArmorComponent>(target, out var armor))
        {
            if (remove)
                _armorCoefficients[target].Remove(resistance);
            else
                _armorCoefficients[target].Add(resistance);

            if (_armorCoefficients[target].Count == 0)
            {
                RemComp<ArmorComponent>(target);
                return;
            }

            var isFirst = true;
            foreach (var list in _armorCoefficients[target])
            {
                foreach (var (type, coefficient) in list.Coefficients)
                {
                    if (isFirst)
                    {
                        totalResistance.Coefficients[type] = Math.Max(_armorBlockers[type], coefficient);
                        isFirst = false;
                    }
                    else
                    {
                        if (totalResistance.Coefficients.TryAdd(type, coefficient))
                        {
                            totalResistance.Coefficients[type] = Math.Max(_armorBlockers[type], totalResistance.Coefficients[type]);
                            continue;
                        }

                        totalResistance.Coefficients[type] -= 1f - coefficient;
                        totalResistance.Coefficients[type] = Math.Max(_armorBlockers[type], totalResistance.Coefficients[type]);
                    }
                }
                foreach (var (type, reduction) in list.FlatReduction)
                {
                    totalResistance.FlatReduction[type] += Math.Max(0, reduction);
                }
            }
        }
        else
        {
            armor = AddComp<ArmorComponent>(target); // Create new instance of component if they don't create before.
            _armorSystem.ChangeExamineState(new Entity<ArmorComponent>(target, armor), true); // Disable examine for others
            List<DamageModifierSet> list = [resistance];
            _armorCoefficients[target] = list;
            totalResistance = resistance;
        }

        var entity = new Entity<ArmorComponent>(target, armor);
        _armorSystem.ChangeComponentModifiers(entity, totalResistance); // Apply All
        _armorSystem.ChangeExamineState(entity,true);
    }
}

internal sealed class BaseSpeed(float walkSpeed, float sprintSpeed)
{
    public readonly float WalkSpeed = walkSpeed;
    public readonly float SprintSpeed = sprintSpeed;
}

// _HORIZON ENDS
