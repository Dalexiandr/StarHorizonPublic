using System;
using System.IO;
using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Shared._Horizon.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorsSpawnSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        private readonly Dictionary<string, string[]> _sponsorItems = new(StringComparer.OrdinalIgnoreCase);
        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("sponsor");
            _sawmill.Info("SponsorsSpawnSystem initialized");
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
            SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        }

        public string[] GetItemsForPlayer(string playerName)
        {
            return _sponsorItems.TryGetValue(playerName, out var items) ? items : Array.Empty<string>();
        }

        private void OnRoundStarting(RoundStartingEvent ev)
        {
            LoadSponsorItems();
        }

        private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
        {
            var playerName = ev.Player.Name;

            var items = GetItemsForPlayer(playerName);
            if (items.Length == 0)
            {
                _sawmill.Debug($"No sponsor items for player {playerName}");
                return;
            }

            if (!TryComp<HandsComponent>(ev.Mob, out var handsComponent))
            {
                _sawmill.Warning($"Player {playerName} has no HandsComponent, cannot give sponsor items");
                return;
            }

            var coords = Transform(ev.Mob).Coordinates;

            foreach (var itemProtoId in items)
            {
                try
                {
                    var spawnedItem = EntityManager.SpawnEntity(itemProtoId, coords);

                    if (!_handsSystem.TryPickupAnyHand(ev.Mob, spawnedItem, handsComp: handsComponent))
                    {
                        _handsSystem.PickupOrDrop(ev.Mob, spawnedItem, handsComp: handsComponent);
                    }

                    _sawmill.Info($"Spawned sponsor item {itemProtoId} for player {playerName}");
                }
                catch (Exception ex)
                {
                    _sawmill.Error($"Failed to spawn sponsor item {itemProtoId} for player {playerName}: {ex.Message}");
                }
            }
        }

        private void LoadSponsorItems()
        {
            try
            {
                _sponsorItems.Clear();

                var sponsorItemsPath = NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemItemsPath));

                if (!_resourceManager.UserData.Exists(sponsorItemsPath))
                {
                    _sawmill.Debug($"Sponsor items file does not exist: {sponsorItemsPath}");
                    return;
                }

                var loadedCount = 0;
                using var reader = _resourceManager.UserData.OpenText(sponsorItemsPath);
                string? line;
                while ((line = reader.ReadLine()) != null)
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
                    loadedCount++;
                    _sawmill.Debug($"Loaded sponsor items for {playerName}: {string.Join(", ", items)}");
                }

                _sawmill.Info($"Loaded sponsor items for {loadedCount} players from file");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to load sponsor items: {ex}");
            }
        }


        private ResPath NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            var normalized = path.TrimStart('/');

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Path cannot be only slashes", nameof(path));

            return new ResPath(normalized).ToRootedPath();
        }
    }
}
