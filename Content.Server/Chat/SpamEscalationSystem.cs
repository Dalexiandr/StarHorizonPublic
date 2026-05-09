using Content.Shared.Chat.Components;
using Robust.Shared.Timing; // ДОБАВЬТЕ ЭТО

namespace Content.Server.Chat;

public sealed class SpamEscalationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly Dictionary<EntityUid, SpamViolationRecord> _violationRecords = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpamProtectionComponent, ComponentRemove>(OnComponentRemove);
    }

    public void RegisterViolation(EntityUid uid)
    {
        if (!_violationRecords.TryGetValue(uid, out var record))
        {
            record = new SpamViolationRecord();
            _violationRecords[uid] = record;
        }

        record.ViolationCount++;
        record.LastViolationTime = _gameTiming.CurTime;
    }

    public void ScheduleReset(EntityUid uid, float resetTimeMinutes)
    {
        Timer.Spawn(TimeSpan.FromMinutes(resetTimeMinutes), () =>
        {
            if (_violationRecords.TryGetValue(uid, out var record))
            {
                record.ViolationCount = 0;
            }
        });
    }

    public float GetEscalatedMuteDuration(EntityUid uid, float baseDuration, SpamProtectionComponent comp)
    {
        if (!_violationRecords.TryGetValue(uid, out var record) || record.ViolationCount <= 1)
            return baseDuration;

        var escalatedDuration = baseDuration * MathF.Pow(comp.EscalationMultiplier, record.ViolationCount - 1);

        return MathF.Min(escalatedDuration, comp.MaxMuteDuration);
    }

    public int GetViolationCount(EntityUid uid)
    {
        return _violationRecords.TryGetValue(uid, out var record) ? record.ViolationCount : 0;
    }

    private void OnComponentRemove(EntityUid uid, SpamProtectionComponent component, ComponentRemove args)
    {
        _violationRecords.Remove(uid);
    }

    private sealed class SpamViolationRecord
    {
        public int ViolationCount { get; set; }
        public TimeSpan LastViolationTime { get; set; }
    }
}
