using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Horizon.SponsorManager
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class ListActiveSponsorsCommand : IConsoleCommand
    {
        [Dependency] private readonly SponsorManager _sponsorManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public string Command => "listsponsors";
        public string Description => "Lists all active sponsors.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var activeSponsors = new List<string>();

            foreach (var session in _playerManager.Sessions)
            {
                if (_sponsorManager.IsSponsor(session.Name))
                {
                    activeSponsors.Add(session.Name);
                }
            }

            shell.WriteLine("Active sponsors:");
            foreach (var sponsor in activeSponsors)
            {
                shell.WriteLine($"- {sponsor}");
            }
        }
    }
}
