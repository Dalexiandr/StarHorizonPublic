using Content.Server.Body.Components;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Xenomorph;

public sealed class XenomorphSystem : SharedXenomorphSystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.xenomorph");
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!; // Goobstation

    public override void Initialize()
    {
        base.Initialize();
        _sawmill.Debug("XenomorphSystem initialized");
    }

    public override void Update(float frameTime)
    {
        // Goobstation start
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenomorphComponent, BloodstreamComponent, BodyComponent>();  // Added BodyComponent to query

        var count = 0;
        while (query.MoveNext(out var uid, out var xenomorph, out var bloodstream, out var body))
        {
            count++;
            if (xenomorph.WeedHeal == null || time < xenomorph.NextPointsAt)
                continue;

            // Update next heal time
            xenomorph.NextPointsAt = time + xenomorph.WeedHealRate;

            if (!xenomorph.OnWeed)
                continue;

            // Apply regular weed healing if on weeds
            _sawmill.Debug($"Update: applying weed heal to uid={uid}");
            _damageable.TryChangeDamage(uid, xenomorph.WeedHeal);

            // Process bleeding and blood loss in parallel with cached values
            ProcessBloodLoss(uid, bloodstream);
        }
        if (count > 0)
            _sawmill.Debug($"Update: processed {count} xenomorphs");
    }

    // Slowly heal bloodloss
    private void ProcessBloodLoss(EntityUid uid, BloodstreamComponent bloodstream)
    {
        _sawmill.Debug($"ProcessBloodLoss: uid={uid}");
        if (!_solutionContainer.ResolveSolution(uid,
                bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution,
                out var bloodSolution)
                || bloodSolution.Volume >= bloodstream.BloodMaxVolume)
        {
            _sawmill.Debug($"ProcessBloodLoss: solution not resolved or volume maxed, volume={bloodSolution?.Volume}");
            return;
        }

        var bloodloss = new DamageSpecifier();
        bloodloss.DamageDict["Bloodloss"] = -0.2f;  // Heal blood per tick
        _damageable.TryChangeDamage(uid, bloodloss);
        _sawmill.Debug($"ProcessBloodLoss: healed bloodloss for uid={uid}");
    }
    // Goobstation end
}
