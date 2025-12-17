using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        private FileSystemWatcher _watcher = default!;

        private readonly string _sponsorsFilePath = "Resources/Prototypes/_Horizon/Sponsors/SponsorInfo/sponsors.txt";
        private readonly string _dsSponsorsFilePath = "Resources/Prototypes/_Horizon/Sponsors/SponsorInfo/discord_sponsors.txt";
        private readonly string _disposableFilePath = "Resources/Prototypes/_Horizon/Sponsors/SponsorInfo/disposable.txt";
        private readonly string _sponsorItemsFilePath = "Resources/Prototypes/_Horizon/Sponsors/SponsorInfo/sponsor_items.txt";

        private static HashSet<string> _sponsors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _sponsorsAndBalances = new();

        #region Check files
        public void LoadSponsorsInfoFile()
        {
            EnsureFileExists(_dsSponsorsFilePath);
            EnsureFileExists(_sponsorsFilePath);
            EnsureFileExists(_disposableFilePath);
            EnsureFileExists(_sponsorItemsFilePath);
        }

        private void EnsureFileExists(string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath!);
            }

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, string.Empty);
            }
        }
        #endregion Check files

        #region File Watcher
        public void FileWatcher()
        {
            _watcher = new FileSystemWatcher()
            {
                Path = Directory.GetCurrentDirectory() + @"/Resources/Prototypes/_Horizon/Sponsors/SponsorInfo",
                Filter = "discord_sponsors.txt",
                NotifyFilter = NotifyFilters.LastWrite,
            };

            _watcher.Changed += SyncSponsorsFiles;
            _watcher.EnableRaisingEvents = true;
        }
        #endregion File Watcher

        #region Discord
        private void SyncSponsorsFiles(object sender, FileSystemEventArgs e)
        {
            ReadSponsorsFile();

            var discordLines = SafeReadAllLines(_dsSponsorsFilePath);

            ProcessDiscordSponsors(discordLines);
        }

        private void ProcessDiscordSponsors(string[] discordLines)
        {
            var discordSponsors = discordLines
                .Select(line => line.Split(',')[1].Trim().ToLowerInvariant())
                .ToHashSet();

            var currentSponsors = _sponsors.Select(s => s.ToLowerInvariant()).ToHashSet();

            foreach (var discordSponsor in discordSponsors)
            {
                var line = discordLines
                    .Select(l => l.Split(','))
                    .FirstOrDefault(parts => parts.Length >= 3 && parts[1].Trim().Equals(discordSponsor, StringComparison.OrdinalIgnoreCase));

                if (line != null && line.Length >= 4)
                {
                    var ckey = line[1].Trim();
                    var discordId = line[2].Trim();
                    var slots = CalculateSlots(discordId);
                    var tokens = CalculateTokens(discordId);

                    if (currentSponsors.Contains(discordSponsor))
                    {
                        SaveSponsors(ckey, slots, tokens);
                    }
                    else
                    {
                        AddSponsor(ckey, slots, tokens);
                    }
                }
            }

            foreach (var sponsor in currentSponsors)
            {
                if (!discordSponsors.Contains(sponsor))
                {
                    var originalSponsorName = _sponsors.FirstOrDefault(s => s.Equals(sponsor, StringComparison.OrdinalIgnoreCase));

                    if (originalSponsorName != null)
                    {
                        RemoveSponsorFromFile(originalSponsorName);
                    }
                }
            }
        }

        private string[] SafeReadAllLines(string filePath, int maxRetries = 3, int delay = 1000)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);

                    var lines = new List<string>();
                    string? line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    return lines.ToArray();
                }
                catch (IOException)
                {
                    Task.Delay(delay).Wait();
                }
            }

            return Array.Empty<string>();
        }


        private int CalculateSlots(string discordId)
        {
            return discordId switch
            {
                "1349080752209395833" => 2,
                "1349080829334257856" => 5,
                "1349080858224623717" => 15,
                "1349080888927064216" => 20,
                "1349080921537773568" => 20,
                "1349080947399725136" => 20,
                _ => 0
            };
        }

        private int CalculateTokens(string discordId)
        {
            return discordId switch
            {
                "1349080829334257856" => 5,
                "1349080858224623717" => 10,
                "1349080888927064216" => 15,
                "1349080921537773568" => 20,
                "1349080947399725136" => 50,
                _ => 0
            };
        }
        #endregion Discord

        #region Read/Write File
        public void ReadSponsorsFile()
        {
            _sponsors.Clear();
            _sponsorsAndBalances.Clear();

            foreach (var line in File.ReadLines(_sponsorsFilePath))
            {
                var parts = line.Split(';');

                var userName = parts[0].Trim();

                if (int.TryParse(parts[2], out var balance))
                {
                    _sponsorsAndBalances[userName] = balance;
                }

                _sponsors.Add(userName);
            }
        }

        public void AddSponsor(string userName, int slot, int token)
        {
            _sponsors.Add(userName);

            SaveSponsors(userName, slot, token);
        }

        public void SaveSponsors(string userName, int slot, int token)
        {
            var lines = File.ReadAllLines(_sponsorsFilePath).ToList();
            var index = lines.FindIndex(line => line.StartsWith(userName, StringComparison.Ordinal));

            if (index != -1)
            {
                lines[index] = $"{userName};{slot};{token}";
            }
            else
            {
                lines.Add($"{userName};{slot};{token}");
            }

            File.WriteAllLines(_sponsorsFilePath, lines);

            _sponsorsAndBalances[userName] = token;
        }

        public void RemoveSponsorFromFile(string userName)
        {
            _sponsors.Remove(userName);
            _sponsorsAndBalances.Remove(userName);

            var lines = File.ReadAllLines(_sponsorsFilePath).ToList();
            var index = lines.FindIndex(line => line.StartsWith(userName, StringComparison.OrdinalIgnoreCase));

            if (index != -1)
            {
                lines.RemoveAt(index);
                File.WriteAllLines(_sponsorsFilePath, lines);
            }
        }
        #endregion Read/Write File

        #region Methods of finding
        public bool IsSponsor(string userName)
        {
            return _sponsors.Contains(userName);
        }

        public int GetCharacterSlots(string userName)
        {
            var maxCharacterSlots = _cfg.GetCVar(CCVars.GameMaxCharacterSlots);

            var line = File.ReadLines(_sponsorsFilePath)
                .FirstOrDefault(l => l.Contains(userName));

            if (line != null)
            {
                var parts = line.Split(';');

                if (int.TryParse(parts[1], out var slot))
                {
                    return maxCharacterSlots + slot;
                }
            }

            return maxCharacterSlots;
        }

        public int GetBalance(string userName)
        {
            if (_sponsorsAndBalances.TryGetValue(userName, out var balance))
            {
                return balance;
            }

            return 0;
        }

        public void UpdateSponsorsAndBalances()
        {
            _sponsorsAndBalances.Clear();

            foreach (var line in File.ReadLines(_sponsorsFilePath))
            {
                var parts = line.Split(';');

                if (parts.Length >= 3)
                {
                    var userName = parts[0].Trim();

                    if (int.TryParse(parts[2], out var balance))
                    {
                        _sponsorsAndBalances[userName] = balance;
                    }
                }
            }


            var disposableLines = SafeReadAllLines(_disposableFilePath);

            foreach (var line in disposableLines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var ckey = parts[0].Trim();
                    if (int.TryParse(parts[2], out var additionalTokens))
                    {
                        if (_sponsorsAndBalances.ContainsKey(ckey))
                        {
                            _sponsorsAndBalances[ckey] += additionalTokens;
                        }
                    }
                }
            }
        }


        public void DeductBalance(string userName, int cost)
        {
            if (_sponsorsAndBalances.TryGetValue(userName, out var balance))
            {
                if (balance >= cost)
                {
                    balance -= cost;
                    _sponsorsAndBalances[userName] = balance;
                }
            }
        }

        public string GetColor(string userName)
        {
            var line = File.ReadLines(_dsSponsorsFilePath)
                .FirstOrDefault(l => l.Contains(userName));

            if (line != null)
            {
                var parts = line.Split(", ");

                return parts[4];
            }

            return "#FF0000";
        }
        #endregion Methods of finding
    }
}
