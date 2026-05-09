using Content.Server.GameTicking.Events;
using Content.Server.Guardian;
using Content.Shared._Horizon.Sponsors.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Log;

namespace Content.Server._Horizon.SponsorManager;

public sealed class SponsorManagerHelper : EntitySystem
{
    [Dependency] private readonly SponsorManager _sponsorManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("sponsor");
        _sawmill.Info("SponsorManagerHelper initialized");

        SubscribeNetworkEvent<SponsorCheckRequestEvent>(OnSponsorCheckRequest);
        SubscribeNetworkEvent<SponsorBuyItemRequestEvent>(OnSponsorBuyItemRequest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        _sawmill.Info("Round starting: syncing Discord sponsors, then updating balances");
        _sponsorManager.SyncDiscordSponsorsAtRoundStart();
        _sponsorManager.UpdateSponsorsAndBalances();
    }

    private void OnSponsorCheckRequest(SponsorCheckRequestEvent ev, EntitySessionEventArgs args)
    {
        var playerName = args.SenderSession.Name;
        var isSponsor = _sponsorManager.IsSponsor(ev.SponsorName);
        var balance = _sponsorManager.GetBalance(ev.SponsorName);

        _sawmill.Info($"Player {playerName} (sponsor name: {ev.SponsorName}) attempted to open sponsor shop. Is sponsor: {isSponsor}, Balance: {balance}");

        RaiseNetworkEvent(new SponsorCheckResponseEvent(isSponsor, balance), args.SenderSession.Channel);
    }

    private void OnSponsorBuyItemRequest(SponsorBuyItemRequestEvent ev, EntitySessionEventArgs args)
    {
        var playerName = args.SenderSession.Name;

        if (!TryGetEntity(ev.PlayerNetId, out var serverUid))
        {
            _sawmill.Warning($"Player {playerName} attempted to buy item {ev.ItemPrototypeId} but entity {ev.PlayerNetId} not found");
            return;
        }

        if (!EntityManager.HasComponent<CanHostGuardianComponent>(serverUid))
        {
            _sawmill.Warning($"Player {playerName} attempted to buy item {ev.ItemPrototypeId} but missing CanHostGuardianComponent");
            return;
        }

        var currentBalance = _sponsorManager.GetBalance(ev.SponsorName);

        if (currentBalance >= ev.Cost)
        {
            _sponsorManager.DeductBalance(ev.SponsorName, ev.Cost);
            SpawnItem(serverUid.Value, ev.ItemPrototypeId);

            var updatedBalance = _sponsorManager.GetBalance(ev.SponsorName);
            _sawmill.Info($"Player {playerName} (sponsor: {ev.SponsorName}) successfully bought item {ev.ItemPrototypeId} for {ev.Cost} tokens. New balance: {updatedBalance}");

            RaiseNetworkEvent(new SponsorBuyItemResponseEvent(updatedBalance), args.SenderSession.Channel);
        }
        else
        {
            _sawmill.Warning($"Player {playerName} (sponsor: {ev.SponsorName}) attempted to buy item {ev.ItemPrototypeId} for {ev.Cost} tokens but has insufficient balance: {currentBalance}");
            var updatedBalance = _sponsorManager.GetBalance(ev.SponsorName);
            RaiseNetworkEvent(new SponsorBuyItemResponseEvent(updatedBalance), args.SenderSession.Channel);
        }
    }

    private void SpawnItem(EntityUid playerUid, string itemPrototypeId)
    {
        try
        {
            if (EntityManager.TryGetComponent(playerUid, out HandsComponent? hands))
            {
                var item = EntityManager.SpawnEntity(itemPrototypeId, Transform(playerUid).Coordinates);

                var handsSystem = EntityManager.System<SharedHandsSystem>();

                if (!handsSystem.TryPickupAnyHand(playerUid, item, handsComp: hands))
                    handsSystem.PickupOrDrop(playerUid, item, handsComp: hands);

                _sawmill.Debug($"Spawned item {itemPrototypeId} for player {playerUid}");
            }
            else
            {
                _sawmill.Warning($"Failed to spawn item {itemPrototypeId} for player {playerUid}: no HandsComponent");
            }
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to spawn item {itemPrototypeId} for player {playerUid}: {ex}");
        }
    }
}
