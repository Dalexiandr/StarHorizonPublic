using Content.Shared.Movement.Systems;

namespace Content.Shared._Horizon.FoodBoost;

public abstract class SharedFoodBoostSystem : EntitySystem
{
    [Dependency] protected readonly MovementSpeedModifierSystem MoveSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodMovespeedBoostComponent, ComponentShutdown>(OnMoveSpeedShutdown);
        SubscribeLocalEvent<FoodMovespeedBoostComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    private void OnMoveSpeedShutdown(Entity<FoodMovespeedBoostComponent> ent, ref ComponentShutdown args)
    => MoveSpeed.RefreshMovementSpeedModifiers(ent.Owner);

    private void OnRefreshMoveSpeed(Entity<FoodMovespeedBoostComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    => args.ModifySpeed(ent.Comp.Modifier);
}
