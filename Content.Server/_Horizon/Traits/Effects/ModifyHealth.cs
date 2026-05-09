using System.Linq;
using Content.Shared._Horizon.Traits;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Horizon.Traits;

public sealed partial class ModifyHealth : BaseTraitEffect
{
    [DataField]
    public Dictionary<MobState, int> HealthChange = new();

    public override void DoEffect(EntityUid uid, IEntityManager entMan)
    {
        var thresholdsSys = entMan.System<MobThresholdSystem>();

        var dict = HealthChange.Select(x => ((int)x.Key, x.Value)).ToDictionary();
        for (var i = 0; i < HealthChange.Count; i++)
        {
            var (state, change) = HealthChange.ElementAt(i);
            if (!thresholdsSys.TryGetThresholdForState(uid, state, out var threshold))
                continue;

            var newThreshold = Math.Max((int)threshold.Value + change, 10);

            thresholdsSys.SetMobStateThreshold(uid, newThreshold, state);
        }
    }
}
