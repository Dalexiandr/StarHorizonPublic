using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.EventItems;

/// <summary>
/// Represents a single event item, synced between server and client.
/// </summary>
[Serializable, NetSerializable]
public sealed class EventItemData
{
    /// <summary>
    /// Database record ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The base entity prototype ID.
    /// </summary>
    public string PrototypeId { get; set; } = string.Empty;

    /// <summary>
    /// Custom entity name if modified, otherwise null.
    /// </summary>
    public string? CustomName { get; set; }

    /// <summary>
    /// Custom entity description if modified, otherwise null.
    /// </summary>
    public string? CustomDescription { get; set; }

    /// <summary>
    /// Credit cost for this item.
    /// </summary>
    public int CreditCost { get; set; }

    /// <summary>
    /// Maximum number of uses. Null means permanent (unlimited).
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Remaining uses. Null means permanent (unlimited).
    /// </summary>
    public int? RemainingUses { get; set; }

    /// <summary>
    /// Whether the player has enabled this item for spawn.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Admin username who granted this item.
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this item was granted.
    /// </summary>
    public DateTime GrantedAt { get; set; }
}
