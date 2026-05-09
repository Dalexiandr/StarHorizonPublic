// Maded by Gorox. Discord - smeshinka112

using Content.Server.Atmos.Components;
using Content.Shared._Horizon.XenoPotion.Components;
using Content.Shared._Horizon.XenoPotionEffected.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction;

namespace Content.Server._Horizon.Xenobiology;

public sealed class XenoPotionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, XenoPotionComponent component, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            args.Target == null ||
            !TryComp<XenoPotionEffectedComponent>(uid, out var potionEffected))
            return;

        var target = args.Target.Value;
        var name = MetaData(args.Target.Value).EntityName;

        if (!TryComp<PressureProtectionComponent>(target, out var pressureComp) ||
            !HasComp<ClothingComponent>(target))
            return;

        switch (component.Effect)
        {
            case "Speed":
                _metaData.SetEntityName(target, Loc.GetString("potion-speed-name-prefix", ("target", name)));
                potionEffected.Color = component.Color;
                break;

            case "Pressure":
                _metaData.SetEntityName(args.Target.Value, Loc.GetString("potion-pressure-name-prefix", ("target", name)));
                potionEffected.Color = component.Color;
                pressureComp.LowPressureMultiplier = 1000f;
                break;
        }

        EntityManager.DeleteEntity(args.Used);
        args.Handled = true;
    }
}
