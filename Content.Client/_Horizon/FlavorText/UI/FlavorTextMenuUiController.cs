using Content.Shared._Horizon.FlavorText;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Horizon.FlavorText.UI;

public sealed partial class FlavorTextMenuUiController : UIController
{
    private FlavorTextMenu? _menu;

    public void OpenMenu(EntityUid? ent, string name, string icDesc, string oocDesc, ErpStatus erp)
    {
        if (_menu == null)
        {
            _menu = UIManager.CreateWindow<FlavorTextMenu>();
            _menu.OpenCentered();
        }

        _menu.OnClose += () => _menu = null;

        _menu.Populate(ent, name, icDesc, oocDesc, erp);
    }
}
