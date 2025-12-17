namespace Content.Server._Horizon.Medical.Limbs;

[ByRefEvent]
public record struct LimbAttachedEvent<T>(EntityUid Limb, T Comp)
{
    public readonly EntityUid Limb = Limb;
    public readonly T Comp = Comp;
}

[ByRefEvent]
public record struct LimbRemovedEvent<T>(EntityUid Limb, T Comp)
{
    public readonly EntityUid Limb = Limb;
    public readonly T Comp = Comp;
}
