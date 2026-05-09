using System.Text.Json.Serialization;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;


public sealed partial class PainFadeEffect : EntityEffect
{
    [DataField(required: true)]
    [JsonPropertyName("fade")]
    public DamageSpecifier Fade = new();

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString($"pain-fade-effect");

    public override void Effect(EntityEffectBaseArgs args)
    {
        var ev = new PainFadeEffectParams(args.TargetEntity, Fade);
        args.EntityManager.EventBus.RaiseLocalEvent(args.TargetEntity, ref ev, true);
    }
}

[ByRefEvent]
public record struct PainFadeEffectParams(EntityUid Target, DamageSpecifier Specifier);
