using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server._NF.Bank;
using Content.Server.Database;
using Content.Shared._NF.Bank.Components;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;

namespace Content.Server._Horizon.EventItems;

/// <summary>
/// Spawns event items when a player completes spawning.
/// Handles component override application and bank deductions.
/// </summary>
public sealed class EventItemsSpawnSystem : EntitySystem
{
    /// <summary>
    /// Components that must not be overridden on spawned entities.
    /// Overwriting these on initialized entities corrupts runtime state (broadphase, physics, etc.)
    /// </summary>
    private static readonly HashSet<string> SkippedComponents = new()
    {
        "Transform",
        "MetaData",
        "Physics",
        "Fixtures",
        "Broadphase",
        "PhysicsMap",
        "ContainerManager",
        "ActorComponent",
        "Eye",
        "Appearance",
    };

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("event-items-spawn");
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private async void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var userId = ev.Player.UserId.UserId;

        List<HorizonAdminLoadout> items;
        try
        {
            items = await _db.GetAdminLoadoutItemsAsync(userId);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to load event items for player {ev.Player.Name}: {ex}");
            return;
        }

        // Filter: enabled AND (permanent OR has remaining uses)
        var enabledItems = items
            .Where(i => i.IsEnabled && (i.RemainingUses == null || i.RemainingUses > 0))
            .ToList();
        _sawmill.Debug($"Player {ev.Player.Name} has {items.Count} event items total, {enabledItems.Count} enabled and available.");

        if (enabledItems.Count == 0)
            return;

        // Get current bank balance after regular loadout deductions
        var hasBankComp = TryComp<BankAccountComponent>(ev.Mob, out var bankComp);

        if (!TryComp<HandsComponent>(ev.Mob, out var handsComponent))
        {
            _sawmill.Warning($"Player {ev.Player.Name} has no HandsComponent, cannot give event items.");
            return;
        }

        var coords = Transform(ev.Mob).Coordinates;
        var totalSpent = 0;

        foreach (var item in enabledItems)
        {
            try
            {
                // Check if player can afford this item
                if (item.CreditCost > 0)
                {
                    if (!hasBankComp || bankComp!.Balance < item.CreditCost + totalSpent)
                    {
                        _sawmill.Debug($"Player {ev.Player.Name} cannot afford event item {item.PrototypeId} (cost: {item.CreditCost})");
                        continue;
                    }
                }

                // Validate prototype exists
                if (!_protoManager.HasIndex<EntityPrototype>(item.PrototypeId))
                {
                    _sawmill.Warning($"Event item prototype {item.PrototypeId} not found, skipping.");
                    continue;
                }

                // Spawn the base entity
                var spawnedEntity = EntityManager.SpawnEntity(item.PrototypeId, coords);

                // Apply component overrides if present
                if (!string.IsNullOrWhiteSpace(item.ComponentOverridesYaml))
                {
                    ApplyComponentOverrides(spawnedEntity, item.ComponentOverridesYaml);
                }

                // Apply custom name and description
                if (item.CustomName != null)
                    _metaSystem.SetEntityName(spawnedEntity, item.CustomName);
                if (item.CustomDescription != null)
                    _metaSystem.SetEntityDescription(spawnedEntity, item.CustomDescription);

                // Try to put in hand or drop nearby
                if (!_handsSystem.TryPickupAnyHand(ev.Mob, spawnedEntity, handsComp: handsComponent))
                {
                    _handsSystem.PickupOrDrop(ev.Mob, spawnedEntity, handsComp: handsComponent);
                }

                if (item.CreditCost > 0)
                    totalSpent += item.CreditCost;

                // Decrement remaining uses for limited items
                if (item.RemainingUses != null)
                {
                    await _db.DecrementAdminLoadoutItemUsesAsync(item.Id);
                    _sawmill.Debug($"Decremented uses for event item {item.Id} ({item.PrototypeId}), was {item.RemainingUses}/{item.MaxUses}.");
                }

                _sawmill.Info($"Spawned event item {item.PrototypeId} for player {ev.Player.Name}");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to spawn event item {item.PrototypeId} for player {ev.Player.Name}: {ex}");
            }
        }

        // Deduct total spent from bank
        if (totalSpent > 0 && hasBankComp)
        {
            _sawmill.Info($"Deducting {totalSpent} credits from player {ev.Player.Name}'s bank account for event items.");
            _bankSystem.TryBankWithdraw(ev.Mob, totalSpent);
        }

        _sawmill.Info($"Finished spawning event items for player {ev.Player.Name}. Total cost: {totalSpent} cr.");
    }

    /// <summary>
    /// Applies serialized component overrides to a spawned entity.
    /// Parses the YAML and applies each component's data fields to the entity.
    /// </summary>
    private void ApplyComponentOverrides(EntityUid entity, string yamlOverrides)
    {
        try
        {
            using var reader = new StringReader(yamlOverrides);
            var documents = DataNodeParser.ParseYamlStream(reader).ToList();

            if (documents.Count == 0)
                return;

            var rootNode = documents[0].Root;

            // The root should be a SequenceDataNode containing component mappings
            if (rootNode is not SequenceDataNode sequence)
            {
                _sawmill.Warning($"Expected SequenceDataNode for component overrides, got {rootNode.GetType().Name}");
                return;
            }

            foreach (var node in sequence)
            {
                if (node is not MappingDataNode compMapping)
                    continue;

                if (!compMapping.TryGet("type", out var typeNode))
                    continue;

                var compName = typeNode.ToString();

                // Skip components that would corrupt runtime state if overwritten
                if (SkippedComponents.Contains(compName))
                    continue;

                // Remove the "type" key — it's metadata, not component data
                var dataMapping = compMapping.Copy();
                dataMapping.Remove("type");

                if (dataMapping.Children.Count == 0)
                    continue;

                // Find the component registration
                if (!_compFactory.TryGetRegistration(compName, out var registration))
                {
                    _sawmill.Warning($"Component {compName} not found in factory, skipping override.");
                    continue;
                }

                // Get or add the component on the entity
                if (!EntityManager.TryGetComponent(entity, registration.Type, out var existingComp))
                {
                    // Component doesn't exist on entity — add it
                    existingComp = _compFactory.GetComponent(registration.Type);
                    EntityManager.AddComponent(entity, existingComp);
                }

                // Deserialize the override data into the existing component
                var newComp = _serialization.Read(registration.Type, dataMapping, skipHook: true, notNullableOverride: true);
                if (newComp != null)
                {
                    // Copy the deserialized values to the existing component
                    object? target = existingComp;
                    _serialization.CopyTo(newComp, ref target, skipHook: true, notNullableOverride: true);

                    // Only dirty networked components — server-only ones don't need it
                    if (registration.NetID != null)
                        Dirty(entity, existingComp);
                }
            }
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to apply component overrides: {ex}");
        }
    }
}
