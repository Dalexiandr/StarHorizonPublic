using Content.Client.Eui;
using Content.Shared._Horizon.EventItems;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Horizon.EventItems;

[UsedImplicitly]
public sealed class GrantEventItemEui : BaseEui
{
    private readonly GrantEventItemWindow _window;

    public GrantEventItemEui()
    {
        _window = new GrantEventItemWindow();
        _window.OnClose += OnWindowClosed;
        _window.OnConfirm += OnConfirm;
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is GrantEventItemEuiState grantState)
        {
            _window.UpdateState(grantState);
        }
    }

    private void OnWindowClosed()
    {
        SendMessage(new CloseEuiMessage());
    }

    private void OnConfirm(Guid targetUserId, int creditCost, int? maxUses)
    {
        SendMessage(new GrantEventItemMessage
        {
            TargetPlayerUserId = targetUserId,
            CreditCost = creditCost,
            MaxUses = maxUses,
        });
    }
}
