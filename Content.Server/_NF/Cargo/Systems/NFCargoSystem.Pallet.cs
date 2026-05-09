using Content.Server.Cargo.Components;
using Content.Server._NF.Cargo.Components;
using Content.Server._Horizon.MarketSaturation; // Horizon - насыщение рынка
using Content.Shared._Horizon.MarketSaturation; // Horizon - насыщение рынка
using Content.Shared._NF.Bank.Components;
using Content.Shared._NF.Cargo.BUI;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Events;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Stacks; // Horizon
using Robust.Shared.Audio;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Coordinates;
using Robust.Shared.Random;

namespace Content.Server._NF.Cargo.Systems;

/// <summary>
/// Handles cargo pallet (sale) mechanics.
/// Based off of Wizden's CargoSystem.
/// </summary>
public sealed partial class NFCargoSystem
{
    // The maximum distance from the console to look for pallets.
    private const int DefaultPalletDistance = 8;

    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<NFCargoPalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<NFCargoPalletConsoleComponent, CargoPalletAppraiseMessage>(OnPalletAppraise);
        SubscribeLocalEvent<NFCargoPalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    #region Console

    private void UpdatePalletConsoleInterface(Entity<NFCargoPalletConsoleComponent> ent, EntityUid actor) // Frontier: EntityUid<Entity // Horizon - добавил поле actor
    {
        if (Transform(ent).GridUid is not EntityUid gridUid)
        {
            _ui.SetUiState(ent.Owner, CargoPalletConsoleUiKey.Sale,
            new NFCargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        // Modify prices based on modifier.
        GetPalletGoods(ent, gridUid, actor, out var toSell, out var amount, out var noModAmount, out Dictionary<string, double> additionalCurrency);
        if (TryComp<MarketModifierComponent>(ent, out var priceMod))
        {
            amount *= priceMod.Mod;
        }
        amount += noModAmount;

        _ui.SetUiState(ent.Owner, CargoPalletConsoleUiKey.Sale,
            new NFCargoPalletConsoleInterfaceState((int)amount, toSell.Count, true));
    }

    private void OnPalletUIOpen(Entity<NFCargoPalletConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdatePalletConsoleInterface(ent, args.Actor);  // Horizon - добавил поле actor
    }

    /// <summary>
    /// Ok so this is just the same thing as opening the UI, its a refresh button.
    /// I know this would probably feel better if it were like predicted and dynamic as pallet contents change
    /// However.
    /// I dont want it to explode if cargo uses a conveyor to move 8000 pineapple slices or whatever, they are
    /// known for their entity spam i wouldnt put it past them
    /// </summary>

    private void OnPalletAppraise(Entity<NFCargoPalletConsoleComponent> ent, ref CargoPalletAppraiseMessage args)
    {
        UpdatePalletConsoleInterface(ent, args.Actor);  // Horizon - добавил поле actor
    }

    #endregion

    #region Shuttle

    /// <summary>
    /// Calculates distance between two EntityCoordinates
    /// Used to check for cargo pallets around the console instead of on the grid.
    /// </summary>
    /// <param name="point1">first point to get distance between</param>
    /// <param name="point2">second point to get distance between</param>
    /// <returns></returns>
    public static double CalculateDistance(EntityCoordinates point1, EntityCoordinates point2)
    {
        var xDifference = point2.X - point1.X;
        var yDifference = point2.Y - point1.Y;

        return Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
    }

    /// GetCargoPallets(gridUid, BuySellType.Sell) to return only Sell pads
    /// GetCargoPallets(gridUid, BuySellType.Buy) to return only Buy pads
    private List<(EntityUid Entity, CargoPalletComponent Component, TransformComponent PalletXform)> GetCargoPallets(EntityUid consoleUid, EntityUid gridUid, BuySellType requestType = BuySellType.All)
    {
        _pads.Clear();

        if (!TryComp(consoleUid, out TransformComponent? consoleXform))
            return _pads;

        var query = AllEntityQuery<CargoPalletComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            // Short-path easy checks
            if (compXform.ParentUid != gridUid
                || !compXform.Anchored
                || (requestType & comp.PalletType) == 0)
            {
                continue;
            }

            // Check distance on pallets
            var distance = CalculateDistance(compXform.Coordinates, consoleXform.Coordinates);
            var maxPalletDistance = DefaultPalletDistance;

            // Get the mapped checking distance from the console
            if (TryComp<NFCargoPalletConsoleComponent>(consoleUid, out var cargoShuttleComponent))
                maxPalletDistance = cargoShuttleComponent.PalletDistance;

            if (distance > maxPalletDistance)
                continue;

            _pads.Add((uid, comp, compXform));

        }

        return _pads;
    }
    #endregion

    #region Station

    private bool SellPallets(Entity<NFCargoPalletConsoleComponent> consoleUid, EntityUid gridUid, EntityUid actor, out double amount, out double noMultiplierAmount, out Dictionary<string, double> additionalCurrency) // Frontier: first arg to Entity, add noMultiplierAmount    // Horizon - добавил поля actor и cash
    {
        GetPalletGoods(consoleUid, gridUid, actor, out var toSell, out amount, out noMultiplierAmount, out additionalCurrency); // Horizon - добавил поле actor

        Log.Debug($"Cargo sold {toSell.Count} entities for {amount} (plus {noMultiplierAmount} without mods)");

        if (toSell.Count == 0)
            return false;

        var ev = new NFEntitySoldEvent(toSell, gridUid, actor); // Horizon - добавил поле actor
        RaiseLocalEvent(ref ev);

        foreach (var ent in toSell)
            Del(ent);

        return true;
    }

    private void GetPalletGoods(Entity<NFCargoPalletConsoleComponent> consoleUid, EntityUid gridUid, EntityUid actor, out HashSet<EntityUid> toSell, out double amount, out double noMultiplierAmount, out Dictionary<string, double> additionalCurrency) // Frontier: first arg to Entity, add noMultiplierAmount // Horizon - добавил поле actor
    {
        amount = 0;
        noMultiplierAmount = 0;
        toSell = new HashSet<EntityUid>();
        additionalCurrency = new();

        // Horizon: Получаем станцию для применения насыщения рынка
        var owningStation = _station.GetOwningStation(gridUid);
        MarketSaturationComponent? saturationComp = null;
        if (owningStation != null)
            TryComp(owningStation.Value, out saturationComp);

        // Horizon: Первый проход — считаем количество предметов каждого типа на паллетах,
        // чтобы учесть насыщение от текущей продажи в цене.
        var pendingCounts = new Dictionary<string, int>();
        if (saturationComp != null)
        {
            foreach (var (palletUid, _, _) in GetCargoPallets(consoleUid, gridUid, BuySellType.Sell))
            {
                _setEnts.Clear();
                _lookup.GetEntitiesIntersecting(palletUid, _setEnts, LookupFlags.Dynamic | LookupFlags.Sundries);

                foreach (var ent in _setEnts)
                {
                    if (_xformQuery.TryGetComponent(ent, out var xf) && (xf.Anchored || !CanSell(ent, xf)))
                        continue;
                    if (_whitelist.IsWhitelistFail(consoleUid.Comp.Whitelist, ent))
                        continue;
                    if (_blacklistQuery.HasComponent(ent))
                        continue;

                    var proto = MetaData(ent).EntityPrototype?.ID;
                    if (proto == null)
                        continue;

                    var cnt = 1;
                    if (TryComp<StackComponent>(ent, out var stk))
                        cnt = stk.Count;

                    pendingCounts[proto] = pendingCounts.GetValueOrDefault(proto) + cnt;
                }
            }
        }
        // Horizon конец

        foreach (var (palletUid, _, _) in GetCargoPallets(consoleUid, gridUid, BuySellType.Sell))
        {
            // Containers should already get the sell price of their children so can skip those.
            _setEnts.Clear();

            _lookup.GetEntitiesIntersecting(palletUid, _setEnts,
                LookupFlags.Dynamic | LookupFlags.Sundries);

            foreach (var ent in _setEnts)
            {
                // Dont sell:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                // - anything blacklisted (e.g. players).
                if (toSell.Contains(ent) ||
                    _xformQuery.TryGetComponent(ent, out var xform) &&
                    (xform.Anchored || !CanSell(ent, xform)))
                {
                    continue;
                }

                if (_whitelist.IsWhitelistFail(consoleUid.Comp.Whitelist, ent))
                    continue;

                if (_blacklistQuery.HasComponent(ent))
                    continue;

                var price = _pricing.GetPrice(ent, currency: consoleUid.Comp.CashType, user: actor);

                // Horizon: Применяем насыщение рынка и рыночные события
                if (saturationComp != null)
                {
                    var protoId = MetaData(ent).EntityPrototype?.ID;
                    if (protoId != null)
                    {
                        // Насыщение рынка
                        if (pendingCounts.TryGetValue(protoId, out var pendingCount))
                        {
                            var existingSold = saturationComp.SaturationData.TryGetValue(protoId, out var satData)
                                ? satData.TotalSold
                                : 0;
                            var totalWithPending = existingSold + pendingCount;
                            var multiplier = MarketSaturationSystem.GetMultiplier(totalWithPending, saturationComp);
                            price = Math.Round(price * multiplier);
                        }

                        // Рыночные события — модификатор цены от активных событий
                        var eventMod = MarketEventSystem.GetEventModifier(protoId, saturationComp);
                        if (eventMod != 1.0)
                            price = Math.Round(price * eventMod);
                    }
                }
                // Horizon конец

                if (price == 0)
                    continue;
                toSell.Add(ent);

                // Check for items that are immune to market modifiers
                if (HasComp<IgnoreMarketModifierComponent>(ent))
                    noMultiplierAmount += price;
                else
                    amount += price;

                // Check for any additional currency payouts
                if (TryComp(ent, out AdditionalPalletCurrencyComponent? currencyComponent))
                {
                    if (_random.Prob(currencyComponent.SpawnProbability))
                    {
                        if (!additionalCurrency.ContainsKey(currencyComponent.Currency))
                        {
                            additionalCurrency.TryAdd(currencyComponent.Currency, 0);
                        }
                        additionalCurrency[currencyComponent.Currency] += currencyComponent.Amount;
                    }
                }
            }
        }
    }

    private bool CanSell(EntityUid uid, TransformComponent xform)
    {
        // Look for blacklisted items and stop the selling of the container.
        if (_blacklistQuery.HasComponent(uid))
            return false;

        // Allow selling dead mobs
        if (_mobQuery.TryComp(uid, out var mob) && mob.CurrentState != MobState.Dead)
            return false;

        // NOTE: no bounties for now
        // var complete = IsBountyComplete(uid, out var bountyEntities);

        // Recursively check for mobs at any point.
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            // NOTE: no bounties for now
            // if (complete && bountyEntities.Contains(child))
            //     continue;

            if (!CanSell(child, _xformQuery.GetComponent(child)))
                return false;
        }

        return true;
    }

    private void OnPalletSale(Entity<NFCargoPalletConsoleComponent> ent, ref CargoPalletSellMessage args)
    {
        if (!TryComp(ent, out TransformComponent? xform))
            return;

        if (xform.GridUid is not EntityUid gridUid)
        {
            _ui.SetUiState(ent.Owner, CargoPalletConsoleUiKey.Sale,
            new NFCargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        if (!SellPallets(ent, gridUid, args.Actor, out var price, out var noMultiplierPrice, out Dictionary<string, double> additionalCurrency)) // Horizon - добавил поле actor
            return;

        // Handle market modifiers & immune objects
        if (TryComp<MarketModifierComponent>(ent, out var priceMod))
        {
            price *= priceMod.Mod;
        }
        price += noMultiplierPrice;

        var stackPrototype = _proto.Index(ent.Comp.CashType);
        var stackUid = _stack.Spawn((int)price, stackPrototype, args.Actor.ToCoordinates());
        if (!_hands.TryPickupAnyHand(args.Actor, stackUid))
            _transform.SetLocalRotation(stackUid, Angle.Zero); // Orient these to grid north instead of map north

        // Iterate through additional currency payouts, putting them in hand if possible
        foreach (var (currencyId, currencyAmount) in additionalCurrency)
        {
            var currencyUid = _stack.Spawn((int)currencyAmount, currencyId, args.Actor.ToCoordinates());
            if (!_hands.TryPickupAnyHand(args.Actor, currencyUid))
                _transform.SetLocalRotation(currencyUid, Angle.Zero);
        }
        _audio.PlayPvs(ApproveSound, ent);
        UpdatePalletConsoleInterface(ent, args.Actor);  // Horizon - добавил поле actor
    }

    #endregion
}

/// <summary>
/// Event broadcast raised by-ref before it is sold and
/// deleted but after the price has been calculated.
/// </summary>
[ByRefEvent]
public readonly record struct NFEntitySoldEvent(HashSet<EntityUid> Sold, EntityUid Grid, EntityUid Actor); // Horizon - добавил поле Actor
