using Content.Server.Botany.Components;
using Content.Shared.Cargo;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Botany.Systems;

/// <summary>
/// Reduces the price of grown produce by a configurable multiplier.
/// By default, grown vegetables and plants are worth 10 times less than their reagent value.
/// </summary>
public sealed class ProducePricingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    /// <summary>
    /// Price multiplier for produce items. Default is 0.1 (10 times cheaper).
    /// </summary>
    public const float ProducePriceMultiplier = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProduceComponent, PriceCalculationEvent>(OnProducePriceCalculation);
    }

    private void OnProducePriceCalculation(EntityUid uid, ProduceComponent component, ref PriceCalculationEvent args)
    {
        // Calculate the solution-based price manually and apply the multiplier
        var solutionPrice = GetSolutionPrice(uid);
        args.Price = solutionPrice * ProducePriceMultiplier;

        // Mark as handled so the base pricing system doesn't add the full solution price again
        args.Handled = true;
    }

    private double GetSolutionPrice(EntityUid uid)
    {
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solComp))
            return 0.0;

        var price = 0.0;

        foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((uid, solComp)))
        {
            var solution = soln.Comp.Solution;
            foreach (var (reagent, quantity) in solution.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var reagentProto))
                    continue;

                price += (float)quantity * reagentProto.PricePerUnit;
            }
        }

        return price;
    }
}
