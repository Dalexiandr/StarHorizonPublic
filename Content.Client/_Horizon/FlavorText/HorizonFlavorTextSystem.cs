using Content.Client._Horizon.FlavorText.UI;
using Content.Shared._Horizon.FlavorText;
using Content.Shared.IdentityManagement;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.FlavorText;

public sealed partial class HorizonFlavorTextSystem : SharedHorizonFlavorTextSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void OpenFlavorMenu(EntityUid uid, EntityUid user, string description)
    {
        base.OpenFlavorMenu(uid, user, description);

        var oocDesc = CompOrNull<OocDescriptionComponent>(uid)?.Description ?? string.Empty;
        var erpStatus = CompOrNull<ErpStatusComponent>(uid)?.Status ?? ErpStatus.No;

        _ui.GetUIController<FlavorTextMenuUiController>().OpenMenu(uid, Identity.Name(uid, EntityManager), description, oocDesc, erpStatus);
    }
}
