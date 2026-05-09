using Content.Shared._Horizon._Fractions.AnCo.Cryptominer;
using Robust.Client.GameObjects;

namespace Content.Client._Horizon._Fractions.AnCo.Cryptominer;

public sealed class AnCoCryptominerVisualizerSystem : VisualizerSystem<AnCoCryptominerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AnCoCryptominerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<CryptominerState>(uid, CryptominerVisuals.State, out var state, args.Component))
            state = CryptominerState.Off;

        if (!AppearanceSystem.TryGetData<int>(uid, CryptominerVisuals.DiskCount, out var diskCount, args.Component))
            diskCount = 0;

        // Update base layers based on state
        UpdateLevelLayer(args.Sprite, state);

        // Handle lock visibility (no animation)
        UpdateLockLayer(args.Sprite, state);

        // Update disk layer based on count
        UpdateDiskLayer(args.Sprite, diskCount);
    }

    private void UpdateLevelLayer(SpriteComponent sprite, CryptominerState state)
    {
        // Update level overlay based on state
        // level-1 = on/normal, level-2 = warning, level-3 = overheat, level-4 = critical
        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Level, out var levelLayer))
            return;

        if (state == CryptominerState.Off || state == CryptominerState.NoAtmosphere || state == CryptominerState.NoDisks)
        {
            sprite.LayerSetVisible(levelLayer, false);
            return;
        }

        sprite.LayerSetVisible(levelLayer, true);
        var levelState = state switch
        {
            CryptominerState.Normal => "level-1",
            CryptominerState.Warning => "level-2",
            CryptominerState.Overheat => "level-3",
            CryptominerState.Critical => "level-4",
            _ => "level-1"
        };
        sprite.LayerSetState(levelLayer, levelState);
    }

    private void UpdateLockLayer(SpriteComponent sprite, CryptominerState state)
    {
        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Lock, out var lockLayer))
            return;

        // Lock is visible when NOT overheating (Off, Normal, Warning, NoAtmosphere, NoDisks)
        // Lock is hidden when overheating (Overheat, Critical)
        var isOverheating = state == CryptominerState.Overheat || state == CryptominerState.Critical;

        if (isOverheating)
        {
            sprite.LayerSetVisible(lockLayer, false);
        }
        else
        {
            sprite.LayerSetVisible(lockLayer, true);
            sprite.LayerSetState(lockLayer, "lock");
        }
    }

    private void UpdateDiskLayer(SpriteComponent sprite, int diskCount)
    {
        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Disks, out var diskLayer))
            return;

        if (diskCount <= 0)
        {
            sprite.LayerSetVisible(diskLayer, false);
            return;
        }

        sprite.LayerSetVisible(diskLayer, true);

        // disk-1 for 1 disk, disk-2 for 2 disks, etc.
        var diskState = $"disk-{Math.Clamp(diskCount, 1, 4)}";
        sprite.LayerSetState(diskLayer, diskState);
    }
}
