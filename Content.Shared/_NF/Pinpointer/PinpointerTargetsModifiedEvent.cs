using Robust.Shared.GameObjects;

namespace Content.Shared._NF.Pinpointer;

/// <summary>
/// Raised on a pinpointer after its <see cref="Content.Shared.Pinpointer.PinpointerComponent.Targets"/> list was updated.
/// </summary>
[ByRefEvent]
public readonly record struct PinpointerTargetsModifiedEvent(HashSet<EntityUid> PreviousTargets);
