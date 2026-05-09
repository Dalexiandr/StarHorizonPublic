using Content.Server.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void ProduceGrown(EntityUid uid, ProduceComponent produce)
    {
        if (!TryGetSeed(produce, out var seed))
            return;

        // Only process produce if the plant is actually producing (not a seed)
        // If plant has been reset to seed state, don't process mutations
        if (produce.Seed == null || produce.Seed.Maturation <= 0)
            return;

        if (!_solutionContainerSystem.EnsureSolution(uid,
                produce.SolutionName,
                out var solutionContainer,
                FixedPoint2.Zero))
            return;

        // Apply mutations first (they may add to solution)
        foreach (var mutation in seed.Mutations)
        {
            if (mutation.AppliesToProduce)
            {
                var args = new EntityEffectBaseArgs(uid, EntityManager);
                mutation.Effect.Effect(args);
            }
        }

        // Remove only the seed chemicals (from previous growth if any)
        // but preserve chemicals added by mutations
        foreach (var (chem, _) in seed.Chemicals)
        {
            var reagentId = new ReagentId(chem, null);
            if (solutionContainer.ContainsReagent(reagentId))
            {
                var quantity = solutionContainer.GetReagentQuantity(reagentId);
                if (quantity > FixedPoint2.Zero)
                {
                    solutionContainer.RemoveReagent(reagentId, quantity);
                }
            }
        }

        const float MaxProduceVolume = 100f;
        var currentVolume = FixedPoint2.Zero;

        // Calculate current volume from mutation-added chemicals
        foreach (var reagent in solutionContainer.Contents)
        {
            currentVolume += reagent.Quantity;
        }

        // Add seed chemicals in order, replacing older ones if total exceeds max volume
        foreach (var (chem, quantity) in seed.Chemicals)
        {
            var amount = FixedPoint2.New(quantity.Min);
            if (quantity.PotencyDivisor > 0 && seed.Potency > 0)
                amount += FixedPoint2.New(seed.Potency / quantity.PotencyDivisor);
            amount = FixedPoint2.New(MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));

            // Check if adding this chemical would exceed the limit
            if ((currentVolume + amount).Float() > MaxProduceVolume)
            {
                // Calculate how much volume needs to be freed
                var volumeNeeded = currentVolume + amount - FixedPoint2.New(MaxProduceVolume);

                // Remove the oldest chemicals to make room for the new one
                while (volumeNeeded > FixedPoint2.Zero && solutionContainer.Contents.Count > 0)
                {
                    var oldReagent = solutionContainer.Contents[0];
                    var removeAmount = oldReagent.Quantity > volumeNeeded ? volumeNeeded : oldReagent.Quantity;
                    currentVolume -= removeAmount;
                    volumeNeeded -= removeAmount;
                    solutionContainer.RemoveReagent(oldReagent.Reagent, removeAmount);
                }
            }

            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
            currentVolume += amount;
        }
    }

    public void OnProduceExamined(EntityUid uid, ProduceComponent comp, ExaminedEvent args)
    {
        if (comp.Seed == null)
            return;

        using (args.PushGroup(nameof(ProduceComponent)))
        {
            foreach (var m in comp.Seed.Mutations)
            {
                // Don't show mutations that have no effect on produce (sentience)
                if (!m.AppliesToProduce)
                    continue;

                if (m.Description != null)
                    args.PushMarkup(Loc.GetString(m.Description));
            }
        }
    }
}
