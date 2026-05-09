namespace Content.Server._Horizon.Weapons;

public sealed class SpreadModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RangedWeaponSpreadModifiersComponent, GetRecoilModifiersEvent>(OnGetModifier);
    }

    private void OnGetModifier(Entity<RangedWeaponSpreadModifiersComponent> ent, ref GetRecoilModifiersEvent args)
    {
        args.Modifier *= ent.Comp.Modifier;
    }
}
