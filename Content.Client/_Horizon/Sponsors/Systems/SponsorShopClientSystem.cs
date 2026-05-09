using Content.Shared._Horizon.Sponsors.Systems;

namespace Content.Client._Horizon.Sponsors.Systems;

public sealed class SponsorConnectClientSystem : EntitySystem
{
    public void SendSponsorCheckRequest(string playerName)
    {
        var requestEvent = new SponsorCheckRequestEvent(playerName);
        RaiseNetworkEvent(requestEvent);
    }

    public void SendSponsorBuyItemRequest(string playerName, int cost, NetEntity playerNetId, string itemPrototypeId)
    {
        var requestEvent = new SponsorBuyItemRequestEvent(playerName, cost, playerNetId, itemPrototypeId);
        RaiseNetworkEvent(requestEvent);
    }
}
