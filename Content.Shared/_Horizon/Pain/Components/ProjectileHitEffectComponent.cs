namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ProjectileHitEffectComponent : Component
{
    [DataField]
    public byte Gunshots = 1;

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan EffectCooldown = TimeSpan.FromSeconds(6);

    [DataField]
    public string BulletId = "default";

    [DataField]
    public bool Push = false;
}
