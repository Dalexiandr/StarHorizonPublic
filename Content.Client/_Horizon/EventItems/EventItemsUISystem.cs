using System;
using System.Collections.Generic;
using Content.Shared._Horizon.EventItems;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Client._Horizon.EventItems;

/// <summary>
/// Handles incoming event item data from the server and sends requests to the server.
/// </summary>
public sealed class EventItemsUISystem : EntitySystem
{
    private ISawmill _sawmill = default!;

    /// <summary>
    /// Raised when the list of event items is received from the server.
    /// </summary>
    public event Action<List<EventItemData>>? OnItemsReceived;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("event-items-ui");
        SubscribeNetworkEvent<EventItemListMsg>(OnItemList);
    }

    /// <summary>
    /// Sends a request to the server for event items.
    /// </summary>
    public void RequestItems()
    {
        _sawmill.Debug("Requesting event items from server.");
        RaiseNetworkEvent(new EventItemRequestMsg());
    }

    /// <summary>
    /// Sends a toggle request to the server.
    /// </summary>
    public void ToggleItem(int itemId, bool enabled)
    {
        _sawmill.Debug($"Toggling event item {itemId} to {(enabled ? "enabled" : "disabled")}.");
        RaiseNetworkEvent(new EventItemToggleMsg
        {
            ItemId = itemId,
            Enabled = enabled,
        });
    }

    private void OnItemList(EventItemListMsg msg)
    {
        _sawmill.Debug($"Received {msg.Items.Count} event items from server.");
        OnItemsReceived?.Invoke(msg.Items);
    }
}
