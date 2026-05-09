using Content.Shared._Horizon.Expeditions;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Expeditions.Ui;

public sealed partial class ExpeditionGoalsConsoleBoundUserInterface : BoundUserInterface
{
    private ExpeditionGoalsConsoleMenu? _menu;

    public ExpeditionGoalsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ExpeditionGoalsConsoleMenu>();

        if (IoCManager.Resolve<IEntityManager>().TryGetComponent<ExpeditionGoalsConsoleComponent>(Owner, out var component))
            _menu.UpdateSpecifications(component.Categories);

        _menu.OnOptionSelected += optionId =>
        {
            SendMessage(new ClaimExpeditionGoalMessage(optionId, _menu.CurrentSpecification));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ExpeditionGoalsConsoleUiState cast)
            return;

        if (_menu == null)
            return;

        _menu.NextOffer = cast.OfferCooldown;
        _menu.Cooldown = cast.Cooldown;

        _menu.CachedGoals = cast.Goals;
        _menu.Populate();
    }
}
