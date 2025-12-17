using Content.Shared.Silicons.Borgs.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.Borgs;

/// <summary>
/// User interface used by borgs to select their type.
/// </summary>
/// <seealso cref="BorgSelectTypeMenu"/>
/// <seealso cref="BorgSwitchableTypeComponent"/>
/// <seealso cref="BorgSwitchableTypeUiKey"/>
[UsedImplicitly]
public sealed class BorgSelectTypeUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BorgSelectTypeMenu? _menu;

    public BorgSelectTypeUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BorgSelectTypeMenu>();

        // Get the category filter from the borg component if available
        var entityManager = IoCManager.Resolve<IEntityManager>();
        string? categoryFilter = null;

        if (entityManager.TryGetComponent<BorgSwitchableTypeComponent>(Owner, out var comp) && comp.BorgTypeCategory != null)
        {
            categoryFilter = comp.BorgTypeCategory;
        }

        _menu.Initialize(categoryFilter);
        _menu.ConfirmedBorgType += (prototype, skin) => SendPredictedMessage(new BorgSelectTypeMessage(prototype, skin));   // Horizon borg skins
    }
}
