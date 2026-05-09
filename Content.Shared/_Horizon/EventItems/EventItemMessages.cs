using System;
using System.Collections.Generic;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.EventItems;

// ---- Network events for event items tab (client <-> server) ----

/// <summary>
/// Sent by the client to request the list of event items.
/// </summary>
[Serializable, NetSerializable]
public sealed class EventItemRequestMsg : EntityEventArgs
{
}

/// <summary>
/// Sent by the server to provide the list of event items.
/// </summary>
[Serializable, NetSerializable]
public sealed class EventItemListMsg : EntityEventArgs
{
    public List<EventItemData> Items { get; set; } = new();
}

/// <summary>
/// Sent by the client to toggle an event item on/off.
/// </summary>
[Serializable, NetSerializable]
public sealed class EventItemToggleMsg : EntityEventArgs
{
    public int ItemId { get; set; }
    public bool Enabled { get; set; }
}

// ---- EUI state and messages for admin grant dialog ----

/// <summary>
/// State sent from server to client for the grant event item EUI.
/// </summary>
[Serializable, NetSerializable]
public sealed class GrantEventItemEuiState : EuiStateBase
{
    /// <summary>
    /// The entity being granted as an event item.
    /// </summary>
    public NetEntity TargetEntity;

    /// <summary>
    /// Current entity name.
    /// </summary>
    public string EntityName = string.Empty;

    /// <summary>
    /// Current entity description.
    /// </summary>
    public string EntityDescription = string.Empty;

    /// <summary>
    /// Base prototype ID.
    /// </summary>
    public string PrototypeId = string.Empty;

    /// <summary>
    /// List of online players to choose from.
    /// </summary>
    public List<EventItemPlayerInfo> OnlinePlayers = new();
}

/// <summary>
/// Minimal player info for the admin grant dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class EventItemPlayerInfo
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
}

/// <summary>
/// Sent from client to server when admin confirms granting an item.
/// </summary>
[Serializable, NetSerializable]
public sealed class GrantEventItemMessage : EuiMessageBase
{
    public Guid TargetPlayerUserId { get; set; }
    public int CreditCost { get; set; }

    /// <summary>
    /// Maximum number of uses. Null means permanent (unlimited).
    /// </summary>
    public int? MaxUses { get; set; }
}
