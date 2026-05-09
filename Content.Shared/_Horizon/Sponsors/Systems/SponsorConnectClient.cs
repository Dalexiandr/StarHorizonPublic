using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Sponsors.Systems
{
    [Serializable, NetSerializable]
    public sealed class SponsorCheckRequestEvent : EntityEventArgs
    {
        public string SponsorName { get; }

        public SponsorCheckRequestEvent(string sponsorName)
        {
            SponsorName = sponsorName;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SponsorCheckResponseEvent : EntityEventArgs
    {
        public bool IsSponsor { get; }
        public int Balance { get; }

        public SponsorCheckResponseEvent(bool isSponsor, int balance)
        {
            IsSponsor = isSponsor;
            Balance = balance;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SponsorBuyItemRequestEvent : EntityEventArgs
    {
        public string SponsorName { get; }
        public int Cost { get; }
        public NetEntity PlayerNetId { get; }
        public string ItemPrototypeId { get; }

        public SponsorBuyItemRequestEvent(string sponsorName, int cost, NetEntity playerNetId, string itemPrototypeId)
        {
            SponsorName = sponsorName;
            Cost = cost;
            PlayerNetId = playerNetId;
            ItemPrototypeId = itemPrototypeId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SponsorBuyItemResponseEvent : EntityEventArgs
    {
        public int UpdatedBalance { get; }

        public SponsorBuyItemResponseEvent(int updatedBalance)
        {
            UpdatedBalance = updatedBalance;
        }
    }
}
