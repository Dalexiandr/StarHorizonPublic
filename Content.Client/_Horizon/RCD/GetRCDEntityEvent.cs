namespace Content.Client._Horizon.RCD;

[ByRefEvent]
public record struct GetRCDEntityEvent()
{
    public EntityUid? Entity;
};
