namespace Content.Server._Horizon.Weapons;

[RegisterComponent]
public sealed partial class RangedWeaponSpreadModifiersComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
