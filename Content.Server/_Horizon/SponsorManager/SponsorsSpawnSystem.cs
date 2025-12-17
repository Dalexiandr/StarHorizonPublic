using System.IO;
using System.Linq;
using Content.Server.GameTicking.Events;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorsSpawnSystem : EntitySystem
    {
        private readonly Dictionary<string, string[]> _sponsorItems = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        }

        public string[] GetItemsForPlayer(string playerName)
        {
            return _sponsorItems.TryGetValue(playerName, out var items) ? items : Array.Empty<string>();
        }

        private void OnRoundStarting(RoundStartingEvent ev)
        {
            LoadSponsorItems();
        }

        private void LoadSponsorItems()
        {
            _sponsorItems.Clear();

            foreach (var line in File.ReadLines("Resources/Prototypes/_Horizon/Sponsors/SponsorInfo/sponsor_items.txt"))
            {
                var separatorIndex = line.IndexOf(',');
                if (separatorIndex == -1)
                    continue;

                var playerName = line[..separatorIndex].Trim();
                var itemsString = line[(separatorIndex + 1)..].Trim();

                var items = itemsString.Trim('(', ')')
                    .Split(',')
                    .Select(item => item.Trim())
                    .ToArray();

                _sponsorItems[playerName] = items;
            }
        }
    }
}
