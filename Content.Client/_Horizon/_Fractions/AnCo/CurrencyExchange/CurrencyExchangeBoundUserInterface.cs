using Content.Shared._Horizon._Fractions.AnCo.CurrencyExchange;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon._Fractions.AnCo.CurrencyExchange;

public sealed class CurrencyExchangeBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CurrencyExchangeMenu? _menu;

    public CurrencyExchangeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CurrencyExchangeMenu>();
        _menu.SetOwner(this);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CurrencyExchangeBoundUserInterfaceState exchangeState)
        {
            _menu?.UpdateState(exchangeState);
        }
    }

    public void SendExchange(int amount)
    {
        SendMessage(new CurrencyExchangeMessage(amount));
    }

    public void SendExchangeAll()
    {
        SendMessage(new CurrencyExchangeAllMessage());
    }
}
