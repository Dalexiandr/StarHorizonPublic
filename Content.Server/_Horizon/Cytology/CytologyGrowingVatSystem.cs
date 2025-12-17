using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Verbs;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology;
using Content.Server.Fluids.EntitySystems;

namespace Content.Server._Horizon.Cytology;

public sealed class CytologyGrowingVatSystem : SharedCytologyGrowingVatSystem
{

    [Dependency] private readonly SmokeSystem _smokeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<CytologyGrowingVatComponent> growingVat, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        InteractionVerb verb = new()
        {
            Act = growingVat.Comp.IsActive
                ? () => ToggleOff(growingVat)
                : () => ToggleOn(growingVat),
            Text = Loc.GetString("verb-toggle-growing-vat")
        };

        args.Verbs.Add(verb);

    }

    private void ToggleOn(Entity<CytologyGrowingVatComponent> growingVat)
    {
        growingVat.Comp.IsActive = true;
        DirtyField(growingVat.Owner, growingVat.Comp, nameof(growingVat.Comp.IsActive));
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.Working, true);
    }

    private void ToggleOff(Entity<CytologyGrowingVatComponent> growingVat)
    {
        growingVat.Comp.IsActive = false;
        DirtyField(growingVat.Owner, growingVat.Comp, nameof(growingVat.Comp.IsActive));
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.Working, false);
    }
}
