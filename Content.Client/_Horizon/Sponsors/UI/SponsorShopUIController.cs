using System.Threading.Tasks;
using Content.Client._Horizon.Sponsors.Systems;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Horizon.Sponsors.Systems;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Content.Client.Launcher;
using Robust.Shared.Network;

namespace Content.Client._Horizon.Sponsors.UI
{
    [UsedImplicitly]
    public sealed class SponsorShopUIController : UIController
    {
        [Dependency] private readonly SponsorConnectClientSystem _sponsorConnectClientSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ExtendedDisconnectInformationManager _disconnectInfoManager = default!;
        private ISawmill _sawmill = default!;

        private MenuButton? SponsorShopButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.SponsorShop;
        private SponsorShopMenu? _window;
        private int _currentBalance;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SponsorCheckResponseEvent>(OnSponsorReceived);
            SubscribeNetworkEvent<SponsorBuyItemResponseEvent>(OnSponsorBuyItemReceived);
            _disconnectInfoManager.LastNetDisconnectedArgsChanged += OnReconnect;
        }

        private void OnSponsorBuyItemReceived(SponsorBuyItemResponseEvent ev, EntitySessionEventArgs args)
        {
            _currentBalance = ev.UpdatedBalance;
            _window?.UpdateBalance(_currentBalance);
        }

        public void BuyItem(string playerName, int cost, NetEntity playerNetId, string itemPrototypeId)
        {
            _sponsorConnectClientSystem.SendSponsorBuyItemRequest(playerName, cost, playerNetId, itemPrototypeId);
        }

        public void LoadButton()
        {
            CheckSponsorStatus();
        }

        private void CheckSponsorStatus()
        {
            var playerName = _playerManager.LocalSession?.Name;

            if (playerName != null)
            {
                _sponsorConnectClientSystem.SendSponsorCheckRequest(playerName);
            }
        }

        private void OnSponsorReceived(SponsorCheckResponseEvent ev, EntitySessionEventArgs args)
        {
            var isSponsor = ev.IsSponsor;
            _currentBalance = ev.Balance;

            if (SponsorShopButton != null)
            {
                SponsorShopButton.Visible = isSponsor;

                SponsorShopButton.OnPressed -= SponsorShopButtonPressed;

                if (isSponsor)
                    SponsorShopButton.OnPressed += SponsorShopButtonPressed;
                else
                    UnloadButton();
            }
        }

        public void UnloadButton()
        {
            if (SponsorShopButton != null)
                SponsorShopButton.OnPressed -= SponsorShopButtonPressed;
        }

        private void SponsorShopButtonPressed(BaseButton.ButtonEventArgs ev)
        {
            ToggleSponsorShopWindow();
        }

        private void EnsureWindow()
        {
            if (_window is { Disposed: false })
                return;

            if (_window?.Disposed ?? false)
                OnWindowDisposed();

            _window = UIManager.CreateWindow<UI.SponsorShopMenu>();
            _window.OnOpen += OnWindowOpen;
            _window.OnClose += OnWindowClosed;
        }

        private void ToggleSponsorShopWindow()
        {
            if (_window is { IsOpen: true })
            {
                _window.Close();
            }
            else
            {
                EnsureWindow();
                _window?.UpdateBalance(_currentBalance);
                _window?.Open();
            }
        }

        private void OnWindowOpen()
        {
            SponsorShopButton?.SetClickPressed(true);
        }

        private void OnWindowClosed()
        {
            SponsorShopButton?.SetClickPressed(false);
            _window = null;
        }

        private void OnWindowDisposed()
        {
            _window = null;
        }

        private async void OnReconnect(NetDisconnectedArgs? args)
        {
            if (_window?.IsOpen == true)
            {
                _window.Close();
            }

            _window = null;
            SponsorShopButton?.SetClickPressed(false);
            if (SponsorShopButton != null)
                SponsorShopButton.Visible = false;

            await Task.Delay(200);

            try
            {
                UnSubscribeNetworkEvent<SponsorCheckResponseEvent>();
                UnSubscribeNetworkEvent<SponsorBuyItemResponseEvent>();
                SubscribeNetworkEvent<SponsorCheckResponseEvent>(OnSponsorReceived);
                SubscribeNetworkEvent<SponsorBuyItemResponseEvent>(OnSponsorBuyItemReceived);
            }
            catch (Exception ex)
            {
                _sawmill.Error("SponsorShop", $"Error during re-subscription: {ex}");
            }

            CheckSponsorStatus();
        }




    }
}
