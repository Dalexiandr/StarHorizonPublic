using System.Threading;
using Content.Server._NF.Trade;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Shared._NF.Cargo;
using Content.Shared._NF.Trade;
using Content.Shared.Cargo;
using Content.Shared.Examine;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._NF.Cargo.Systems;

public sealed partial class NFCargoSystem
{
    [Dependency] private LabelSystem _label = default!;
    private readonly List<EntityUid> _destinations = new();

    /// <summary>
    /// For cargo orders: all crates from one order get the same destination (one warehouse per batch).
    /// Key: order id, Value: destination station Uid.
    /// </summary>
    private readonly Dictionary<int, EntityUid> _batchDestinationByOrderId = new();

    private void InitializeTradeCrates()
    {
        SubscribeLocalEvent<TradeCrateComponent, PriceCalculationEvent>(OnTradeCrateGetPriceEvent);
        SubscribeLocalEvent<TradeCrateComponent, ComponentInit>(OnTradeCrateInit);
        SubscribeLocalEvent<TradeCrateComponent, ComponentRemove>(OnTradeCrateRemove);
        SubscribeLocalEvent<TradeCrateComponent, ExaminedEvent>(OnTradeCrateExamined);
        SubscribeLocalEvent<TradeCrateComponent, ThrowItemAttemptEvent>(OnTradeCrateThrow);

        SubscribeLocalEvent<TradeCrateDestinationComponent, ComponentInit>(OnDestinationInit);
        SubscribeLocalEvent<TradeCrateDestinationComponent, ComponentRemove>(OnDestinationRemove);
    }

    private void OnTradeCrateGetPriceEvent(Entity<TradeCrateComponent> ent, ref PriceCalculationEvent ev)
    {
        var owningStation = _station.GetOwningStation(ent);
        var atDestination = ent.Comp.DestinationStation != EntityUid.Invalid
                           && owningStation == ent.Comp.DestinationStation
                           || HasComp<TradeCrateWildcardDestinationComponent>(owningStation);
        ev.Price = atDestination ? ent.Comp.ValueAtDestination : ent.Comp.ValueElsewhere;
        if (ent.Comp.ExpressDeliveryTime != null)
        {
            if (_timing.CurTime <= ent.Comp.ExpressDeliveryTime && atDestination)
                ev.Price += ent.Comp.ExpressOnTimeBonus;
            else if (_timing.CurTime > ent.Comp.ExpressDeliveryTime)
                ev.Price -= ent.Comp.ExpressLatePenalty;
        }
        ev.Price = double.Max(0.0, ev.Price); // Ensure non-negative values.
    }

    private void OnTradeCrateInit(Entity<TradeCrateComponent> ent, ref ComponentInit ev)
    {
        // If there are no available destinations, tough luck.
        if (_destinations.Count > 0)
        {
            var randomIndex = _random.Next(_destinations.Count);
            // Better have more than one destination.
            if (_station.GetOwningStation(ent) == _destinations[randomIndex])
            {
                randomIndex = (randomIndex + 1 + _random.Next(_destinations.Count - 1)) % _destinations.Count;
            }
            var destination = _destinations[randomIndex];
            ent.Comp.DestinationStation = destination;
            if (TryComp<TradeCrateDestinationComponent>(destination, out var destComp))
                _appearance.SetData(ent, TradeCrateVisuals.DestinationIcon, destComp.DestinationProto.Id);
            if (TryComp(destination, out MetaDataComponent? metadata))
                _label.Label(ent, metadata.EntityName);
        }

        if (ent.Comp.ExpressDeliveryDuration > TimeSpan.Zero)
        {
            ent.Comp.ExpressDeliveryTime = _timing.CurTime + ent.Comp.ExpressDeliveryDuration;
            _appearance.SetData(ent, TradeCrateVisuals.IsPriority, true);

            ent.Comp.ExpressCancelToken = new CancellationTokenSource();
            Timer.Spawn((int)ent.Comp.ExpressDeliveryDuration.TotalMilliseconds,
                () => DisableTradeCratePriority(ent),
                ent.Comp.ExpressCancelToken.Token);
        }
    }

    private void OnTradeCrateRemove(Entity<TradeCrateComponent> ent, ref ComponentRemove ev)
    {
        ent.Comp.ExpressCancelToken?.Cancel();
    }

    // TODO: move to shared, share delivery time?
    private void OnTradeCrateExamined(Entity<TradeCrateComponent> ent, ref ExaminedEvent ev)
    {
        if (!TryComp(ent.Comp.DestinationStation, out MetaDataComponent? metadata))
            return;

        using (ev.PushGroup(nameof(TradeCrateComponent)))
        {
            ev.PushMarkup(Loc.GetString("trade-crate-destination-station", ("destination", metadata.EntityName)));

            if (ent.Comp.ExpressDeliveryTime == null)
                return;

            ev.PushMarkup(ent.Comp.ExpressDeliveryTime >= _timing.CurTime ?
                Loc.GetString("trade-crate-priority-active") :
                Loc.GetString("trade-crate-priority-inactive"));

            var timeLeft = ent.Comp.ExpressDeliveryTime.Value - _timing.CurTime;
            var timeLeftSeconds = timeLeft.TotalSeconds;
            if (timeLeftSeconds > 1)
                ev.PushMarkup(Loc.GetString("trade-crate-priority-time", ("time", timeLeft.ToString(@"hh\:mm\:ss"))));
            else if (timeLeftSeconds >= 0)
                ev.PushMarkup(Loc.GetString("trade-crate-priority-time-now"));
            else
                ev.PushMarkup(Loc.GetString("trade-crate-priority-past-due", ("time", timeLeft.ToString(@"hh\:mm\:ss"))));
        }
    }

    private void OnTradeCrateThrow(Entity<TradeCrateComponent> ent, ref ThrowItemAttemptEvent ev)
    {
        // Borgs can pick these up, don't let them be thrown.
        ev.Cancelled = true;
    }

    private void DisableTradeCratePriority(EntityUid uid)
    {
        _appearance.SetData(uid, TradeCrateVisuals.IsPriorityInactive, true);
    }

    private void OnDestinationInit(Entity<TradeCrateDestinationComponent> ent, ref ComponentInit ev)
    {
        if (!_destinations.Contains(ent))
            _destinations.Add(ent);
    }

    private void OnDestinationRemove(Entity<TradeCrateDestinationComponent> ent, ref ComponentRemove ev)
    {
        _destinations.Remove(ent);
    }

    private void CleanupTradeCrateDestinations()
    {
        _destinations.Clear();
        _batchDestinationByOrderId.Clear();
    }

    /// <summary>
    /// If the entity is a trade crate spawned as part of an NF cargo order, assign it the batch destination
    /// so that all crates from the same order go to the same warehouse.
    /// </summary>
    public void ApplyBatchDestinationIfTradeCrate(EntityUid entity, NFCargoOrderData order, EntityCoordinates spawn)
    {
        if (!TryComp<TradeCrateComponent>(entity, out var tradeCrate))
            return;

        if (_destinations.Count == 0)
            return;

        var owningStation = _station.GetOwningStation(entity);
        if (!_batchDestinationByOrderId.TryGetValue(order.OrderId, out var destination))
        {
            var randomIndex = _random.Next(_destinations.Count);
            if (_destinations.Count > 1 && owningStation == _destinations[randomIndex])
                randomIndex = (randomIndex + 1 + _random.Next(_destinations.Count - 1)) % _destinations.Count;
            destination = _destinations[randomIndex];
            _batchDestinationByOrderId[order.OrderId] = destination;
        }

        tradeCrate.DestinationStation = destination;
        if (TryComp<TradeCrateDestinationComponent>(destination, out var destComp))
            _appearance.SetData(entity, TradeCrateVisuals.DestinationIcon, destComp.DestinationProto.Id);
        if (TryComp(destination, out MetaDataComponent? metadata))
            _label.Label(entity, metadata.EntityName);

        if (order.NumDispatched >= order.OrderQuantity)
            _batchDestinationByOrderId.Remove(order.OrderId);
    }
}
