using Content.Server.GameTicking.Events;
using Content.Server.Guardian;
using Content.Shared._Horizon.Sponsors.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server._Horizon.SponsorManager;

public sealed class SponsorManagerHelper : EntitySystem
{
    [Dependency] private readonly SponsorManager _sponsorManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sponsorManager.FileWatcher();
        SubscribeNetworkEvent<SponsorCheckRequestEvent>(OnSponsorCheckRequest);
        SubscribeNetworkEvent<SponsorBuyItemRequestEvent>(OnSponsorBuyItemRequest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        _sponsorManager.UpdateSponsorsAndBalances();
    }

    private void OnSponsorCheckRequest(SponsorCheckRequestEvent ev, EntitySessionEventArgs args)
    {
        var isSponsor = _sponsorManager.IsSponsor(ev.SponsorName);
        var balance = _sponsorManager.GetBalance(ev.SponsorName);
        RaiseNetworkEvent(new SponsorCheckResponseEvent(isSponsor, balance), args.SenderSession.Channel);
    }

    private void OnSponsorBuyItemRequest(SponsorBuyItemRequestEvent ev, EntitySessionEventArgs args)
    {
        if (TryGetEntity(ev.PlayerNetId, out var serverUid))
        {
            if (!EntityManager.HasComponent<CanHostGuardianComponent>(serverUid))
                return;

            if (_sponsorManager.GetBalance(ev.SponsorName) >= ev.Cost)
            {
                _sponsorManager.DeductBalance(ev.SponsorName, ev.Cost);

                SpawnItem(serverUid.Value, ev.ItemPrototypeId);
            }

            var updatedBalance = _sponsorManager.GetBalance(ev.SponsorName);
            RaiseNetworkEvent(new SponsorBuyItemResponseEvent(updatedBalance), args.SenderSession.Channel);
        }
    }

    private void SpawnItem(EntityUid playerUid, string itemPrototypeId)
    {
        if (EntityManager.TryGetComponent(playerUid, out HandsComponent? hands))
        {
            var item = EntityManager.SpawnEntity(itemPrototypeId, Transform(playerUid).Coordinates);

            var handsSystem = EntityManager.System<SharedHandsSystem>();

            if (!handsSystem.TryPickupAnyHand(playerUid, item, handsComp: hands))
                handsSystem.PickupOrDrop(playerUid, item, handsComp: hands);
        }
    }
}
