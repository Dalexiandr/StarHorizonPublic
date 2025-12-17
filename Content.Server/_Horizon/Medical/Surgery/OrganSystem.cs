using Content.Server._Horizon.Medical.Limbs;
using Content.Server.Humanoid;
using Content.Shared._Horizon.Medical.Surgery.Components;
using Content.Shared._Horizon.Medical.Surgery.Events;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Speech.Muting;

namespace Content.Server._Horizon.Medical.Surgery;

public sealed class OrganSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = null!;
    [Dependency] private readonly DamageableSystem _damageableSystem = null!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = null!;
    [Dependency] private readonly CyberLimbSystem _cyberLimbSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganImplantationCompleted>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganExtracted>(OnFunctionalOrganExtracted);

        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganImplantationCompleted>(OnEyeImplanted);
        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganExtracted>(OnEyeExtracted);

        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganImplantationCompleted>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganExtracted>(OnTongueExtracted);

        SubscribeLocalEvent<DamageableComponent, SurgeryOrganImplantationCompleted>(OnOrganImplanted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganExtracted>(OnOrganExtracted);

        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganImplantationCompleted>(OnVisualizationImplanted);
        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganExtracted>(OnVisualizationExtracted);
    }

    //

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (ent.Comp.IncreasedSpeed is not null)
            _cyberLimbSystem.IncreaseSpeed(args.Body, ent.Comp.IncreasedSpeed.Value);

        if (ent.Comp.IncreasedArmor is not null)
            _cyberLimbSystem.IncreaseArmor(args.Body, ent.Comp.IncreasedArmor);

        if (ent.Comp.Components is not null)
            EntityManager.AddComponents(args.Body, ent.Comp.Components, false);
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (ent.Comp.IncreasedSpeed is not null)
            _cyberLimbSystem.IncreaseSpeed(args.Body, ent.Comp.IncreasedSpeed.Value, true);

        if (ent.Comp.IncreasedArmor is not null )
            _cyberLimbSystem.IncreaseArmor(args.Body, ent.Comp.IncreasedArmor, true);

        if (ent.Comp.Components is not null)
            EntityManager.RemoveComponents(args.Body, ent.Comp.Components);
    }

    //

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
            || damageRule.InsertDamage is null
            || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable))
            return;

        _damageableSystem.TryChangeDamage(args.Body, damageRule.InsertDamage, true, false, bodyDamageable);
    }
    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
         || damageRule.RemoveDamage is null
         || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable))
            return;

        _damageableSystem.TryChangeDamage(args.Body, damageRule.RemoveDamage, true, false, bodyDamageable);
    }

    /*

    private void OnAbductorOrganImplanted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            victim.Organ = ent.Comp.Organ;
        if (ent.Comp.Organ == AbductorOrganType.Vent)
            AddComp<VentCrawlerComponent>(args.Body);
    }
    private void OnAbductorOrganExtracted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            if (victim.Organ == ent.Comp.Organ)
                victim.Organ = AbductorOrganType.None;

        if (ent.Comp.Organ == AbductorOrganType.Vent)
            RemComp<VentCrawlerComponent>(args.Body);
    }

    */

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        //if (HasComp<AbductorComponent>(args.Body) || !ent.Comp.IsMuted) Комментировано, потому, что нет генокрада
        if (!ent.Comp.IsMuted)
            return;

        ent.Comp.IsMuted = false;
        RemComp<MutedComponent>(args.Body);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref SurgeryOrganExtracted args)
    {
        ent.Comp.IsMuted = true;
        if (HasComp<MutedComponent>(args.Body))
            return;

        AddComp<MutedComponent>(args.Body);
    }

    //

    private void OnEyeExtracted(Entity<OrganEyesComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable))
            return;

        ent.Comp.EyeDamage = blindable.EyeDamage;
        ent.Comp.MinDamage = blindable.MinDamage;
        _blindable.UpdateIsBlind((args.Body, blindable));
    }
    private void OnEyeImplanted(Entity<OrganEyesComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable))
            return;

        _blindable.SetMinDamage((args.Body, blindable), ent.Comp.MinDamage ?? 0);
        _blindable.AdjustEyeDamage((args.Body, blindable), (ent.Comp.EyeDamage ?? 0) - blindable.MaxDamage);
    }

    //

    private void OnVisualizationExtracted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganExtracted args)
    {
        _humanoidAppearanceSystem.SetLayersVisibility(args.Body, [ent.Comp.Layer], false);
    }

    private void OnVisualizationImplanted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        _humanoidAppearanceSystem.SetLayersVisibility(args.Body, [ent.Comp.Layer], true);
        _humanoidAppearanceSystem.SetBaseLayerId(args.Body, ent.Comp.Layer, ent.Comp.Prototype);
    }
}
