using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Main loot table for salvage expeditions.
    /// </summary>
    public static readonly ProtoId<SalvageLootPrototype> ExpeditionsLootProto = "NFSalvageLootModerate"; // Frontier: SalvageLoot<NFSalvageLootModerate

    public string GetFTLName(LocalizedDatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{Loc.GetString(dataset.Values[random.Next(dataset.Values.Count)])}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

    public SalvageMission GetMission(SalvageMissionType config, SalvageDifficultyPrototype difficulty, int seed) // Frontier: add config
    {
        // This is on shared to ensure the client display for missions and what the server generates are consistent
        var modifierBudget = difficulty.ModifierBudget;
        var rand = new System.Random(seed);

        // Run budget in order of priority
        // - Biome
        // - Lighting
        // - Atmos
        // Frontier: filter biomes by difficulty
        var biome = GetBiomeModForDifficulty(difficulty.ID, rand, ref modifierBudget);
        var light = GetBiomeMod<SalvageLightMod>(biome.ID, rand, ref modifierBudget);
        var temp = GetBiomeMod<SalvageTemperatureMod>(biome.ID, rand, ref modifierBudget);
        var air = GetBiomeMod<SalvageAirMod>(biome.ID, rand, ref modifierBudget);
        var dungeon = GetBiomeMod<SalvageDungeonModPrototype>(biome.ID, rand, ref modifierBudget);
        // Frontier: restrict factions per difficulty
        // var factionProtos = _proto.EnumeratePrototypes<SalvageFactionPrototype>().ToList();
        var factionProtos = _proto.EnumeratePrototypes<SalvageFactionPrototype>()
            .Where(x =>
                {
                    return !x.Configs.TryGetValue("Difficulties", out var difficulties)
                        || string.IsNullOrWhiteSpace(difficulties)
                        || difficulties.Split(",").Contains(difficulty.ID.ToString());
                }
            ).ToList();
        // End Frontier: difficulties per faction
        factionProtos.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        var faction = factionProtos[rand.Next(factionProtos.Count)];

        var mods = new List<string>();

        if (air.Description != string.Empty)
        {
            mods.Add(Loc.GetString(air.Description));
        }

        // only show the description if there is an atmosphere since wont matter otherwise
        if (temp.Description != string.Empty && !air.Space)
        {
            mods.Add(Loc.GetString(temp.Description));
        }

        if (light.Description != string.Empty)
        {
            mods.Add(Loc.GetString(light.Description));
        }

        TimeSpan duration;
        if (biome.MinDuration.HasValue && biome.MaxDuration.HasValue)
        {
            var durationSeconds = rand.Next(biome.MinDuration.Value, biome.MaxDuration.Value + 1);
            duration = TimeSpan.FromSeconds(durationSeconds);
        }
        else
        {
            duration = TimeSpan.FromSeconds(CfgManager.GetCVar(CCVars.SalvageExpeditionDuration));
        }

        return new SalvageMission(seed, dungeon.ID, faction.ID, biome.ID, air.ID, temp.Temperature, light.Color, duration, mods, difficulty.ID, config);
    }

    public T GetBiomeMod<T>(string biome, System.Random rand, ref float rating) where T : class, IPrototype, IBiomeSpecificMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Cost > rating || (mod.Biomes != null && !mod.Biomes.Contains(biome)))
                continue;

            rating -= mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }

    public T GetMod<T>(System.Random rand, ref float rating) where T : class, IPrototype, ISalvageMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Cost > rating)
                continue;

            rating -= mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Frontier: Get biome mod filtered by difficulty.
    /// If biome has difficulties list, only use it for those difficulties.
    /// If biome has no difficulties list, use it for all difficulties EXCEPT those that have dedicated biomes.
    /// </summary>
    public SalvageBiomeModPrototype GetBiomeModForDifficulty(string difficultyId, System.Random rand, ref float rating)
    {
        var allBiomes = _proto.EnumeratePrototypes<SalvageBiomeModPrototype>().ToList();

        // Check if any biome is specifically for this difficulty
        var dedicatedBiomes = allBiomes
            .Where(b => b.Difficulties != null && b.Difficulties.Contains(difficultyId))
            .ToList();

        List<SalvageBiomeModPrototype> availableBiomes;

        if (dedicatedBiomes.Count > 0)
        {
            // Use only dedicated biomes for this difficulty
            availableBiomes = dedicatedBiomes;
        }
        else
        {
            // Use biomes that have no difficulty restriction
            availableBiomes = allBiomes
                .Where(b => b.Difficulties == null || b.Difficulties.Count == 0)
                .ToList();
        }

        availableBiomes.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(availableBiomes);

        foreach (var biome in availableBiomes)
        {
            if (biome.Cost > rating)
                continue;

            rating -= biome.Cost;
            return biome;
        }

        throw new InvalidOperationException($"No valid biome found for difficulty {difficultyId}");
    }
}

// Frontier: salvage mission type
[Serializable, NetSerializable]
public enum SalvageMissionType : byte
{
    /// <summary>
    /// Destroy the specified structures in a dungeon.
    /// </summary>
    Destruction = 0,

    /// <summary>
    /// Kill a large creature in a dungeon.
    /// </summary>
    Elimination = 1,

    /// <summary>
    /// Maximum value for random generation, should not be used directly.
    /// </summary>
    Max = Elimination,

    /// <summary>
    /// Combined mission: both destroy structures AND kill megafauna.
    /// Used for Inferno difficulty.
    /// </summary>
    Combined = 2,
}
// End Frontier
