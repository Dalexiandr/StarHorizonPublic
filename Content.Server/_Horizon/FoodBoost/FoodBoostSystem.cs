using System.Linq;
using Content.Shared._Horizon.FoodBoost;
using Content.Shared.Cargo.Components;
using Content.Shared.Damage;
using Content.Shared.Fluids.Components;
using Content.Shared.Nutrition;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.FoodBoost;

public sealed class FoodBoostSystem : SharedFoodBoostSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float MidMoveSpeedMod = 1.05f;
    private const float AdvancedMoveSpeedMod = 1.1f;

    private const float MidRegenDuration = 30f;
    private const float AdvancedRegenDuration = 60f;
    private DamageSpecifier _regenAmount = new()
    {
        DamageDict = new()
        {
            { "Blunt", -0.6 },
            { "Slash", -0.6 },
            { "Piercing", -0.6 },
            { "Heat", -0.6 },
            { "Cold", -0.6 },
            { "Poison", -1 },
        }
    };

    private const double PriceBonus = 200;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantBoostOnConsumeComponent, AfterFullyEatenEvent>(OnMoveSpeedGrant);
    }

    private void OnMoveSpeedGrant(Entity<GrantBoostOnConsumeComponent> ent, ref AfterFullyEatenEvent args)
    {
        if (ent.Comp.MoveSpeedModifier.HasValue)
        {
            var moveSpeedBoost = EnsureComp<FoodMovespeedBoostComponent>(args.User);
            moveSpeedBoost.Modifier = ent.Comp.MoveSpeedModifier.Value;
            moveSpeedBoost.End = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Duration);
            Dirty(args.User, moveSpeedBoost);
            MoveSpeed.RefreshMovementSpeedModifiers(args.User);
        }

        if (ent.Comp.RegenAmount != null)
        {
            var regen = EnsureComp<FoodRegenBoostComponent>(args.User);
            regen.Regen = ent.Comp.RegenAmount;
            regen.End = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Duration);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateMoveSpeed();
        UpdateRegen();
    }

    private void UpdateMoveSpeed()
    {
        var query = EntityQueryEnumerator<FoodMovespeedBoostComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.End <= _timing.CurTime)
                RemCompDeferred(uid, comp);
        }
    }

    private void UpdateRegen()
    {
        var query = EntityQueryEnumerator<FoodRegenBoostComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.End <= _timing.CurTime)
                RemCompDeferred(uid, comp);
        }
    }

    public void ApplyBoost(EntityUid target, bool advancedBoost)
    {
        var coords = Transform(target).Coordinates;
        var dirty = _lookup.GetEntitiesInRange<PuddleComponent>(coords, 3.4f).Count > 1 &&
                    _lookup.GetEntitiesInRange<TagComponent>(coords, 3.4f).Count(x => _tag.HasTag(x.Owner, "Trash")) > 4;

        if (!advancedBoost && dirty)
            return;

        var comp = EnsureComp<GrantBoostOnConsumeComponent>(target);
        comp.Advanced = advancedBoost && !dirty;

        if (_random.Prob(0.5f))
            comp.MoveSpeedModifier = comp.Advanced ? AdvancedMoveSpeedMod : MidMoveSpeedMod;
        else
        {
            comp.RegenAmount = _regenAmount;
            comp.Duration = comp.Advanced ? AdvancedRegenDuration : MidRegenDuration;
        }

        var staticPrice = EnsureComp<StaticPriceComponent>(target);
        staticPrice.Price += PriceBonus;

        Dirty(target, comp);
    }
}
