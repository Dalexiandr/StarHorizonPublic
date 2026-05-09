using System.Diagnostics.CodeAnalysis;
using Content.Shared._Horizon.WorldItem;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

namespace Content.Client._Horizon.WorldItem;

/// <summary>
/// Выясняет находиться ли объект в данный момент на полу или нет.
/// </summary>
public sealed class WorldItemSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSys = null!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<WorldItemComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WorldItemComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WorldItemComponent, ComponentAdd>(OnAdd);
        SubscribeLocalEvent<WorldItemComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<WorldItemComponent, EntParentChangedMessage>(OnParentChanged);
    }

    #region Component Events
    private void OnInit(Entity<WorldItemComponent> entity, ref ComponentInit _)
    {
        ComponentStateChange(entity, false);
    }

    private void OnShutdown(Entity<WorldItemComponent> entity, ref ComponentShutdown _)
    {
        ComponentStateChange(entity,true);
    }

    private void OnAdd(Entity<WorldItemComponent> entity, ref ComponentAdd _)
    {
        ComponentStateChange(entity, false);

    }

    private void OnRemove(Entity<WorldItemComponent> entity, ref ComponentRemove _)
    {
        ComponentStateChange(entity,true);
    }

    private void OnParentChanged(Entity<WorldItemComponent> entity, ref EntParentChangedMessage _)
    {
        ChangeItemSprite(entity);
    }
    #endregion

    private void ComponentStateChange(Entity<WorldItemComponent> entity, bool? delete)
    {
        if (delete is null || !TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        if (delete.Value)
        {
            foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state);
            }
        }
        else
        {
            var layerNumber = 0;
            foreach (var layer in sprite.AllLayers)
            {
                if (layer.RsiState.Name == null)
                    continue;

                var state = layer.RsiState.Name;
                entity.Comp.DefaultSpriteStates[layerNumber] = state;
                layerNumber++;
            }
        }
        ChangeItemSprite(entity);
    }

    private void ChangeItemSprite(Entity<WorldItemComponent> entity)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite) || entity.Comp.DefaultSpriteStates.Count == 0)
            return;

        if (TryComp<AppearanceComponent>(entity.Owner, out var appearance) && !entity.Comp.Continue)
        {
            _appearanceSys.QueueUpdate(entity.Owner, appearance);
            return;
        }

        SetWorldState(entity.Owner, sprite, entity.Comp);
    }

    // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public void SetWorldState(EntityUid uid, SpriteComponent sprite, WorldItemComponent? worldItem = null)
    {
        if (worldItem is null && !TryComp(uid, out worldItem))
            return;

        var transform = Transform(uid);
        if (transform == null)
            return;

        var parent = transform.ParentUid;
        if (parent == null)
            return;

        if (HasComp<BroadphaseComponent>(parent)
            || HasComp<MapComponent>(parent)
            || HasComp<MapGridComponent>(parent))
            ChangeSpriteStates(sprite, worldItem, worldItem.Prefix, worldItem.OverridePrefix);
        else
        {
            foreach (var (layer, state) in worldItem.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state);
            }
        }
    }

    public bool GetWorldState(EntityUid uid,
        [NotNullWhen(true)] out string? prefix,
        [NotNullWhen(true)] out Dictionary<int, string>? spriteStates)
    {
        if (!TryComp<WorldItemComponent>(uid, out var worldItem))
        {
            prefix = null;
            spriteStates = null;
            return false;
        }

        spriteStates = worldItem.DefaultSpriteStates;
        prefix = worldItem.Prefix;
        var transform = Transform(uid);
        if (transform == null)
            return false;

        var parent = transform.ParentUid;
        if (parent == null)
            return false;

        return HasComp<BroadphaseComponent>(parent)
               || HasComp<MapComponent>(parent)
               || HasComp<MapGridComponent>(parent);
    }

    private static void ChangeSpriteStates(SpriteComponent sprite ,WorldItemComponent worldItem, string? prefix, string? overridePrefix)
    {
        if (overridePrefix != null)
        {
            foreach (var (layer, _) in worldItem.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, overridePrefix);
            }
        }
        else
        {
            foreach (var (layer, state) in worldItem.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state + prefix);
            }
        }
    }

    public string GetPrefixOrOverride(EntityUid uid)
    {
        if (TryComp<WorldItemComponent>(uid, out var worldItem) && GetWorldState(uid, out _, out _))
        {
            return worldItem.OverridePrefix ?? worldItem.Prefix;
        }
        return string.Empty;
    }
}
