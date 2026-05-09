using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Cargo;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server._Horizon.Shipyard;

public sealed class ShipyardCostModifierSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipyardEntityPriceMarkerComponent, PriceCalculationEvent>(OnGetEntityCost);
        SubscribeLocalEvent<ShipyardTileAtmosPriceMarkerComponent, PriceCalculationEvent>(OnGetAtmosCost);
        SubscribeLocalEvent<ShipyardGridRequiresNewThrustersComponent, GetAdditionalGridCostEvent>(OnGetThrustersCost);
        SubscribeLocalEvent<SpipyardGridRequiresRustClearingComponent, MapInitEvent>(OnRustInit);
        SubscribeLocalEvent<SpipyardGridRequiresRustClearingComponent, GetAdditionalGridCostEvent>(OnGetRustCost);
    }

    private void OnGetEntityCost(Entity<ShipyardEntityPriceMarkerComponent> ent, ref PriceCalculationEvent args)
    {
        args.Handled = true;

        var entities = _lookup.GetEntitiesInRange<TagComponent>(Transform(ent.Owner).Coordinates, ent.Comp.Radius);
        var count = 0;

        foreach (var item in entities)
        {
            if (!_receiver.IsPowered(item))
                continue;

            if (_tag.HasTag(item.Owner, ent.Comp.Tag))
                count++;
        }

        if (count < ent.Comp.Count)
            return;

        args.Price += ent.Comp.PriceAdded;
    }

    private void OnGetAtmosCost(Entity<ShipyardTileAtmosPriceMarkerComponent> ent, ref PriceCalculationEvent args)
    {
        args.Handled = true;

        if (Transform(ent.Owner).GridUid is not { Valid: true } grid)
            return;

        if (_atmos.IsTileMixtureProbablySafe(grid, grid, Transform(ent.Owner).Coordinates.Position.Floored()))
            args.Price += ent.Comp.PriceAdded;
    }

    private void OnGetThrustersCost(Entity<ShipyardGridRequiresNewThrustersComponent> ent, ref GetAdditionalGridCostEvent args)
    {
        if (!TryComp<MapGridComponent>(ent.Owner, out var gridComp))
            return;

        var ents = Transform(ent.Owner).ChildEnumerator;
        List<Direction> directions = new()
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West
        };

        while (ents.MoveNext(out var child))
        {
            if (!TryComp<ThrusterComponent>(child, out var thruster) || thruster.Type != ThrusterType.Linear)
                continue;

            if (!_receiver.IsPowered(child))
                continue;

            if (_tag.HasTag(child, "RustedThruster"))
                continue;

            var xform = Transform(child);
            if (!xform.Anchored)
                continue;

            var direction = xform.LocalRotation.Opposite().ToWorldVec().GetDir();
            if (directions.Contains(direction))
                directions.Remove(direction);
        }

        switch (directions.Count)
        {
            case 0:
                args.Price += ent.Comp.PriceAdded;
                break;
            case 1:
                args.Price += (int)(ent.Comp.PriceAdded / 3);
                break;
            default:
                break;
        }
    }
    private void OnRustInit(Entity<SpipyardGridRequiresRustClearingComponent> ent, ref MapInitEvent args)
    {
        var ents = Transform(ent.Owner).ChildEnumerator;

        while (ents.MoveNext(out var child))
        {
            if (_tag.HasTag(child, "RustedWall"))
                ent.Comp.StartingRustWalls++;
        }
    }

    private void OnGetRustCost(Entity<SpipyardGridRequiresRustClearingComponent> ent, ref GetAdditionalGridCostEvent args)
    {
        if (!TryComp<MapGridComponent>(ent.Owner, out var gridComp))
            return;

        var ents = Transform(ent.Owner).ChildEnumerator;
        var count = 0;

        while (ents.MoveNext(out var child))
        {
            if (_tag.HasTag(child, "RustedThruster"))
                count++;
        }

        if (count >= ent.Comp.StartingRustWalls / 2)
            return;

        args.Price += count <= 1 ? ent.Comp.PriceAdded : ent.Comp.PriceAdded / 2;
    }
}
