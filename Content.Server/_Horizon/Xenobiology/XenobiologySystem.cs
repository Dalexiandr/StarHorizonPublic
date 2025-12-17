// Maded by Gorox. Discord - smeshinka112

using Content.Server.Polymorph.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Horizon.Xenobiology;

public sealed class XenoBiologySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = null!;
    [Dependency] private readonly IRobustRandom _robustRandom = null!;
    [Dependency] private readonly PolymorphSystem _polymorph = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoBiologyComponent, ComponentAdd>(OnComponentAdd);
        SubscribeLocalEvent<XenoBiologyComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<XenoBiologyComponent, MeleeHitEvent>(OnSlimeAttack);
    }

    private void OnComponentAdd(Entity<XenoBiologyComponent> entity, ref ComponentAdd args)
    {
        entity.Comp.CurrentSpecies = MetaData(entity.Owner).EntityPrototype?.ID;
        CheckPointForSplit(entity.Owner, entity.Comp);
    }

    private void OnComponentInit(Entity<XenoBiologyComponent> entity, ref ComponentInit args)
    {
        entity.Comp.CurrentSpecies = MetaData(entity.Owner).EntityPrototype?.ID;
        CheckPointForSplit(entity.Owner, entity.Comp);
    }

    private void OnSlimeAttack(EntityUid uid, XenoBiologyComponent component, ref MeleeHitEvent args)
    {
        foreach (var hitEntity in args.HitEntities)
        {
            if (!HasComp<XenoFoodComponent>(hitEntity))
                continue;

            if (_mobState.IsIncapacitated(hitEntity))
                continue;

            component.Points += component.PointsPerAttack;
            break;
        }
        CheckPointForSplit(uid, component);
    }

    private void CheckPointForSplit(EntityUid uid, XenoBiologyComponent component)
    {
        if (component.CurrentSpecies is null) // Мы не знаем что делить.
            return;

        if (component.Points < component.TargetToSplitPoints)
            return;

        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
        {
            _polymorph.PolymorphEntity(uid, component.PolymorphEntity); // Превращаем слайма в человика.
            return;
        }

        TrySplitOrMutate(uid, component, Transform(uid).Coordinates);
    }

    public void TrySplitOrMutate(EntityUid uid, XenoBiologyComponent component, EntityCoordinates coordinates)
    {
        if (_robustRandom.Prob(component.MutationChance))
        {
            Spawn(component.MutationEntity, coordinates);
            component.Points = 0;
        }
        else
        {
            if (_robustRandom.Prob(component.SplitChance))
            {
                Spawn(component.CurrentSpecies, coordinates);
                component.Points = 0;
            }
            else
                component.Points -= component.PointLoss;
        }
    }
}
