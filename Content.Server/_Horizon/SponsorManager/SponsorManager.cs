using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Horizon.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        private ISawmill _sawmill = default!;

        private FileSystemWatcher? _fileWatcher;
        private Timer? _debounceTimer;
        private const int DebounceDelayMs = 500;

        private ResPath _dsSponsorsFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemDiscordSponsorsPath));
        private ResPath _disposableFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemDisposablePath));
        private ResPath _sponsorItemsFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemItemsPath));

        private ResPath NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            var normalized = path.TrimStart('/');

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Path cannot be only slashes", nameof(path));

            return new ResPath(normalized).ToRootedPath();
        }

        private static HashSet<string> _sponsors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _sponsorsAndBalances = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _sponsorSlots = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _sponsorColors = new(StringComparer.OrdinalIgnoreCase);

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("sponsor");
            _sawmill.Info("SponsorManager initialized successfully");
        }

        #region Check files
        public void LoadSponsorsInfoFile()
        {
            try
            {
                EnsureFileExists(_dsSponsorsFilePath);
                EnsureFileExists(_disposableFilePath);
                EnsureFileExists(_sponsorItemsFilePath);
                _sawmill.Info("Sponsor system files checked/created successfully");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to load sponsor info files: {ex}");
                throw;
            }
        }

        private void EnsureFileExists(ResPath filePath)
        {
            try
            {
                // Log the path being processed for debugging
                _sawmill.Debug($"Ensuring file exists: {filePath}");

                // Log UserData root directory if available
                var rootDir = _resourceManager.UserData.RootDir;
                if (rootDir != null)
                {
                    _sawmill.Debug($"UserData root directory: {rootDir}");
                }

                // Create directory if it doesn't exist
                _resourceManager.UserData.CreateDir(filePath.Directory);

                // Create file if it doesn't exist
                if (!_resourceManager.UserData.Exists(filePath))
                {
                    _resourceManager.UserData.WriteAllText(filePath, string.Empty);
                    _sawmill.Debug($"Created empty sponsor file: {filePath}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                var rootDir = _resourceManager.UserData.RootDir ?? "unknown (virtual provider)";
                _sawmill.Error($"Permission denied when creating file: {filePath}. " +
                              $"UserData root: {rootDir}. " +
                              $"Error: {ex.Message}. " +
                              $"Make sure the server process has write permissions to the UserData directory.");
                throw;
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to ensure file exists: {filePath}, Error: {ex}");
                throw;
            }
        }
        #endregion Check files

        #region File watching
        public void StartWatching()
        {
            try
            {
                var rootDir = _resourceManager.UserData.RootDir;
                if (rootDir == null)
                {
                    _sawmill.Warning("Cannot start file watcher: UserData.RootDir is null (virtual provider). " +
                                     "Sponsor hot-reload will not be available.");
                    return;
                }

                var relativePath = _dsSponsorsFilePath.ToRelativeSystemPath();
                var fullPath = Path.GetFullPath(Path.Combine(rootDir, relativePath));
                var directory = Path.GetDirectoryName(fullPath);
                var fileName = Path.GetFileName(fullPath);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                {
                    _sawmill.Error($"Cannot start file watcher: invalid path derived from {_dsSponsorsFilePath}");
                    return;
                }

                if (!Directory.Exists(directory))
                {
                    _sawmill.Warning($"Cannot start file watcher: directory does not exist: {directory}");
                    return;
                }

                StopWatching();

                _fileWatcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += OnFileChanged;
                _fileWatcher.Created += OnFileChanged;
                _fileWatcher.Deleted += OnFileChanged;

                _sawmill.Info($"File watcher started for: {fullPath}");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to start file watcher: {ex}");
            }
        }

        public void StopWatching()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Changed -= OnFileChanged;
                _fileWatcher.Created -= OnFileChanged;
                _fileWatcher.Deleted -= OnFileChanged;
                _fileWatcher.Dispose();
                _fileWatcher = null;
                _sawmill.Debug("File watcher stopped");
            }

            if (_debounceTimer != null)
            {
                _debounceTimer.Dispose();
                _debounceTimer = null;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                _ => _taskManager.RunOnMainThread(ResyncSponsors),
                null,
                DebounceDelayMs,
                Timeout.Infinite);
        }

        /// <summary>
        /// Additive-only resync: reads discord_sponsors.txt and adds only NEW sponsors
        /// that are not yet in memory. Existing sponsors are never touched or removed.
        /// Removal of sponsors only happens at round start via SyncDiscordSponsorsAtRoundStart().
        /// Must be called on the main thread.
        /// </summary>
        public void ResyncSponsors()
        {
            try
            {
                _sawmill.Info("Hot-reload triggered: checking for new sponsors in file...");

                var newBalances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var newSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var newColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // Read file and collect only sponsors NOT already in memory
                var discordLines = SafeReadAllLines(_dsSponsorsFilePath);
                foreach (var line in discordLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 3)
                        continue;

                    var originalCkey = parts[1].Trim();
                    var normalizedCkey = NormalizeUserName(originalCkey);
                    var discordId = parts[2].Trim();

                    if (string.IsNullOrWhiteSpace(normalizedCkey) || string.IsNullOrWhiteSpace(discordId))
                        continue;

                    // Skip sponsors already in memory
                    if (_sponsors.Contains(normalizedCkey))
                        continue;

                    newSlots[normalizedCkey] = CalculateSlots(discordId);
                    newBalances[normalizedCkey] = CalculateTokens(discordId);

                    if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
                        newColors[normalizedCkey] = parts[4].Trim();
                }

                if (newBalances.Count == 0)
                {
                    _sawmill.Info("Hot-reload: no new sponsors found in file");
                    return;
                }

                // Apply disposable.txt bonus tokens to new sponsors only
                var disposableLines = SafeReadAllLines(_disposableFilePath);
                foreach (var line in disposableLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 3)
                        continue;

                    var ckey = NormalizeUserName(parts[0].Trim());
                    if (string.IsNullOrWhiteSpace(ckey))
                        continue;

                    if (int.TryParse(parts[2], out var additionalTokens) && newBalances.ContainsKey(ckey))
                        newBalances[ckey] += additionalTokens;
                }

                // Add new sponsors to memory
                foreach (var (name, balance) in newBalances)
                {
                    _sponsors.Add(name);
                    _sponsorsAndBalances[name] = balance;
                    _sponsorSlots[name] = newSlots.GetValueOrDefault(name);

                    if (newColors.TryGetValue(name, out var color))
                        _sponsorColors[name] = color;
                }

                _sawmill.Info($"Hot-reload: added {newBalances.Count} new sponsor(s): {string.Join(", ", newBalances.Keys)}");
                _sawmill.Info($"Hot-reload complete. Total sponsors in memory: {_sponsors.Count}");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to resync sponsors during hot-reload: {ex}");
            }
        }
        #endregion File watching

        #region Discord (sync only at round start)
        /// <summary>
        /// Синхронизирует список спонсоров из discord_sponsors в память.
        /// Вызывается при старте раунда и при старте сервера. Данные только в памяти (без sponsors.txt).
        /// </summary>
        public void SyncDiscordSponsorsAtRoundStart()
        {
            try
            {
                var rootDir = _resourceManager.UserData.RootDir;
                _sawmill.Info($"Syncing Discord sponsors (memory only)...");
                _sawmill.Info($"Sponsors file path: {_dsSponsorsFilePath}, UserData root: {rootDir}");

                _sponsors.Clear();
                _sponsorsAndBalances.Clear();
                _sponsorSlots.Clear();
                _sponsorColors.Clear();

                var discordLines = SafeReadAllLines(_dsSponsorsFilePath);
                _sawmill.Debug($"Read {discordLines.Length} lines from discord_sponsors");

                ProcessDiscordSponsors(discordLines);
                _sawmill.Info($"Discord sponsors sync completed. Total sponsors in memory: {_sponsors.Count}");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to sync Discord sponsors: {ex}");
            }
        }

        private void ProcessDiscordSponsors(string[] discordLines)
        {
            var count = 0;
            foreach (var line in discordLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 3)
                    continue;

                var originalCkey = parts[1].Trim();
                var normalizedCkey = NormalizeUserName(originalCkey);
                var discordId = parts[2].Trim();

                if (string.IsNullOrWhiteSpace(normalizedCkey) || string.IsNullOrWhiteSpace(discordId))
                    continue;

                var slots = CalculateSlots(discordId);
                var tokens = CalculateTokens(discordId);

                // Parse color from parts[4] if available (format: something,ckey,discordId,something,#color)
                string? color = null;
                if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
                {
                    color = parts[4].Trim();
                }

                SetSponsorData(originalCkey, slots, tokens, color);
                count++;
            }

            _sawmill.Info($"Discord sync: {count} sponsors loaded into memory");
        }

        private string[] SafeReadAllLines(ResPath filePath, int maxRetries = 3, int delay = 1000)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var sr = _resourceManager.UserData.OpenText(filePath);

                    var lines = new List<string>();
                    string? line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    return lines.ToArray();
                }
                catch (IOException ex)
                {
                    if (attempt < maxRetries - 1)
                    {
                        _sawmill.Debug($"Failed to read file {filePath} (attempt {attempt + 1}/{maxRetries}), retrying...");
                        Task.Delay(delay).Wait();
                    }
                    else
                    {
                        _sawmill.Error($"Failed to read file {filePath} after {maxRetries} attempts: {ex}");
                    }
                }
            }

            return Array.Empty<string>();
        }


        private int CalculateSlots(string discordId)
        {
            return discordId switch
            {
                "1349080752209395833" => 2, // Спонсор I - Авантюрист
                "1349080829334257856" => 5, // Спонсор II - Наемник
                "1349080858224623717" => 10, // Спонсор III - Шериф
                "1349080888927064216" => 20, // Спонсор IV - Представитель
                "1349080921537773568" => 20, // Спонсор V - Легенда
                "1349080947399725136" => 20, // Спонсор VI - пока нету
                _ => 0
            };
        }

        private int CalculateTokens(string discordId)
        {
            return discordId switch
            {
                "1349080829334257856" => 10, // Спонсор II - Наемник
                "1349080858224623717" => 15, // Спонсор III - Шериф
                "1349080888927064216" => 30, // Спонсор IV - Представитель
                "1349080921537773568" => 50, // Спонсор V - Легенда
                "1349080947399725136" => 100, // Спонсор VI - пока нету
                _ => 0
            };
        }
        #endregion Discord

        #region Memory-only sponsor data
        /// <summary>
        /// Обновляет данные спонсора только в памяти (без записи в файл).
        /// </summary>
        private void SetSponsorData(string userName, int slot, int token, string? color = null)
        {
            var normalizedName = NormalizeUserName(userName);
            _sponsors.Add(normalizedName);
            _sponsorSlots[normalizedName] = slot;
            _sponsorsAndBalances[normalizedName] = token;

            if (!string.IsNullOrWhiteSpace(color))
            {
                _sponsorColors[normalizedName] = color;
            }
        }

        public void AddSponsor(string userName, int slot, int token)
        {
            try
            {
                SetSponsorData(userName, slot, token);
                _sawmill.Info($"Added new sponsor: {userName} (slots: {slot}, tokens: {token})");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to add sponsor {userName}: {ex}");
                throw;
            }
        }

        public void RemoveSponsor(string userName)
        {
            var normalizedName = NormalizeUserName(userName);
            _sponsors.Remove(normalizedName);
            _sponsorsAndBalances.Remove(normalizedName);
            _sponsorSlots.Remove(normalizedName);
            _sponsorColors.Remove(normalizedName);
            _sawmill.Debug($"Removed sponsor from memory: {userName}");
        }
        #endregion Memory-only sponsor data

        #region Methods of finding
        private string NormalizeUserName(string userName)
        {
            return userName?.Trim() ?? string.Empty;
        }

        public bool IsSponsor(string userName)
        {
            var normalizedName = NormalizeUserName(userName);
            return _sponsors.Contains(normalizedName);
        }

        public int GetCharacterSlots(string userName)
        {
            var maxCharacterSlots = _cfg.GetCVar(CCVars.GameMaxCharacterSlots);
            var normalizedName = NormalizeUserName(userName);

            if (_sponsorSlots.TryGetValue(normalizedName, out var slot))
            {
                return maxCharacterSlots + slot;
            }

            return maxCharacterSlots;
        }

        public int GetBalance(string userName)
        {
            var normalizedName = NormalizeUserName(userName);
            if (_sponsorsAndBalances.TryGetValue(normalizedName, out var balance))
            {
                return balance;
            }

            return 0;
        }

        /// <summary>
        /// Добавляет дополнительные токены из disposable.txt к текущим балансам в памяти.
        /// Вызывается после SyncDiscordSponsorsAtRoundStart (при старте раунда и при старте сервера).
        /// </summary>
        public void UpdateSponsorsAndBalances()
        {
            try
            {
                var disposableLines = SafeReadAllLines(_disposableFilePath);
                var additionalTokensCount = 0;

                foreach (var line in disposableLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 3)
                        continue;

                    var ckey = NormalizeUserName(parts[0].Trim());
                    if (string.IsNullOrWhiteSpace(ckey))
                        continue;

                    if (int.TryParse(parts[2], out var additionalTokens))
                    {
                        if (_sponsorsAndBalances.ContainsKey(ckey))
                        {
                            _sponsorsAndBalances[ckey] += additionalTokens;
                            additionalTokensCount++;
                        }
                    }
                }

                _sawmill.Info($"Updated balances: {additionalTokensCount} additional tokens from disposable applied");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to update sponsors and balances: {ex}");
                throw;
            }
        }


        public void DeductBalance(string userName, int cost)
        {
            var normalizedName = NormalizeUserName(userName);
            if (_sponsorsAndBalances.TryGetValue(normalizedName, out var balance))
            {
                if (balance >= cost)
                {
                    balance -= cost;
                    _sponsorsAndBalances[normalizedName] = balance;
                    _sawmill.Debug($"Deducted {cost} tokens from {userName}, new balance: {balance}");
                }
                else
                {
                    _sawmill.Warning($"Insufficient balance for {userName}: has {balance}, needs {cost}");
                }
            }
            else
            {
                _sawmill.Warning($"Attempted to deduct balance for non-sponsor: {userName}");
            }
        }

        /// <summary>
        /// Возвращает цвет спонсора из кеша. Если цвет не установлен, возвращает null.
        /// </summary>
        public string? GetColor(string userName)
        {
            var normalizedName = NormalizeUserName(userName);

            if (_sponsorColors.TryGetValue(normalizedName, out var color))
            {
                return color;
            }

            return "#FF0000";
        }
        #endregion Methods of finding
    }
}
