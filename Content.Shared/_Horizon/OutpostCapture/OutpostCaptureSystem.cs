using System.Diagnostics.CodeAnalysis;
using Content.Shared._Horizon.FlavorText;
using Content.Shared._Horizon.OutpostCapture.Components;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
// ReSharper disable BadListLineBreaks

namespace Content.Shared._Horizon.OutpostCapture;

public class SharedOutpostCaptureSystem : EntitySystem
{
    [Dependency] protected readonly INetManager NetMan = null!;
    [Dependency] protected readonly IRobustRandom Random = null!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = null!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = null!;
    [Dependency] protected readonly SharedUserInterfaceSystem UiSystem = null!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = null!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = null!;

    protected ISawmill Sawmill = null!;

    public override void Initialize()
    {
        SawmillInit();

        SubscribeLocalEvent<OutpostConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<OutpostConsoleComponent, ComponentRemove>(OnConsoleRemove);

        SubscribeLocalEvent<OutpostConsoleComponent, EntInsertedIntoContainerMessage>(OnGetInsertedAttempt);
        SubscribeLocalEvent<OutpostConsoleComponent, EntRemovedFromContainerMessage>(OnGetRemovedAttempt);

        SubscribeLocalEvent<OutpostCaptureComponent, ComponentInit>(OnOutpostInit);
        SubscribeLocalEvent<OutpostCaptureComponent, ComponentRemove>(OnOutpostRemove);

        Subs.BuiEvents<OutpostConsoleComponent>(CaptureUIKey.Key,
        subs =>
        {
            subs.Event<OpenBoundInterfaceMessage>((uid, comp, _) => UpdateConsole((uid, comp)));
            subs.Event<OutpostCaptureButtonPressed>((uid, comp, _) => OnButtonPressed((uid, comp)));
        });
    }

    #region Init
    private void SawmillInit()
    {
        Sawmill = LogManager.GetSawmill("OutpostCapture");
        Sawmill.Info("_outpostCaptureSystem start his work!");
    }

    private void OnConsoleInit(Entity<OutpostConsoleComponent> console, ref ComponentInit args)
    {
        if (TryGetOutpost(console, out var outpost, out var outpostUid))
            return;

        if (console.Comp.CanUseAsSpawnPoint &&
            TransformSystem.TryGetMapOrGridCoordinates(console, out var coords))
            outpost.SpawnLocation = coords;

        console.Comp.LinkedOutpost = GetNetEntity(outpostUid);
        outpost.LinkedConsoles.Add(GetNetEntity(console));
        ItemSlotsSystem.AddItemSlot(console, console.Comp.ContainerSlot, console.Comp.IdCardSlot);
    }

    private void OnConsoleRemove(Entity<OutpostConsoleComponent> console, ref ComponentRemove args)
    {
        if (TryGetOutpost(console, out var outpost, out _) || !TryGetNetEntity(console, out var netConsole))
            return;

        console.Comp.LinkedOutpost = null;
        outpost.LinkedConsoles.Remove(netConsole.Value);
        outpost.CapturingConsoles.Remove(netConsole.Value);
        outpost.CapturedConsoles.Remove(netConsole.Value);
        ItemSlotsSystem.RemoveItemSlot(console, console.Comp.IdCardSlot);
    }

    private void OnGetInsertedAttempt(Entity<OutpostConsoleComponent> console, ref EntInsertedIntoContainerMessage args)
    {
        UpdateConsole(console);
    }

    private void OnGetRemovedAttempt(Entity<OutpostConsoleComponent> console, ref EntRemovedFromContainerMessage args)
    {
        UpdateConsole(console);
    }

    private void OnOutpostInit(Entity<OutpostCaptureComponent> outpost, ref ComponentInit args)
    {
        if (outpost.Comp.NeedCaptured <= 0)
        {
            RemComp<OutpostCaptureComponent>(outpost);
            return;
        }

        if (outpost.Comp.OutpostName == "empty")
            outpost.Comp.OutpostName = MetaData(outpost).EntityName;

        outpost.Comp.NextSpawn = outpost.Comp.SpawnCooldown;
    }

    private void OnOutpostRemove(Entity<OutpostCaptureComponent> outpost, ref ComponentRemove args)
    {
        if (outpost.Comp.LinkedConsoles.Count <= 0 ||
            outpost.Comp.CapturedConsoles.Count <= 0)
            return;

        UnlinkAllConsoles(outpost.Comp);
    }

    public void UnlinkAllConsoles(OutpostCaptureComponent outpost)
    {
        foreach (var console in outpost.LinkedConsoles)
        {
            if (TryGetEntity(console, out var actualConsole) ||
                !TryComp<OutpostConsoleComponent>(actualConsole, out var consoleComp))
                continue;

            consoleComp.LinkedOutpost = null;
            consoleComp.CapturedFaction = null;
            consoleComp.CapturedFactionName = null;
            consoleComp.CapturingTime = null;
            Dirty(actualConsole.Value, consoleComp);
        }
    }
    #endregion

    #region Private methods
    private bool TryGetOutpost(EntityUid uid,
        [NotNullWhen(false)] out OutpostCaptureComponent? outpost,
        [NotNullWhen(false)] out EntityUid? grid)
    {
        outpost = null;
        grid = TransformSystem.GetGrid(Transform(uid).Coordinates);
        return grid == null || !TryComp(grid, out outpost);
    }

    #region Messages
    private void UpdateConsole(Entity<OutpostConsoleComponent> console)
    {
        var progress = UpdateProgressBar(console);
        var buttonStateDisabled = !CanChangeCaptureState(console, out _);
        var labelState = FormattedLabel(console);
        var buttonState = FormattedButton(console);

        var state = new OutpostUIState(progress, buttonStateDisabled, labelState, buttonState);
        UiSystem.SetUiState(console.Owner, CaptureUIKey.Key, state);
    }

    private void OnButtonPressed(Entity<OutpostConsoleComponent> console)
    {
        ChangeCaptureState(console);
        UpdateConsole(console); // Update console
    }
    #endregion

    #region Formats
    private string FormattedLabel(Entity<OutpostConsoleComponent> console)
    {
        switch (console.Comp.State)
        {
            case OutpostConsoleState.Uncaptured:
                return $"{Loc.GetString("faction-uncaptured-state")}";

            case OutpostConsoleState.Capturing:
                return $"{Loc.GetString("faction-capturing-state-by")} {Loc.GetString("faction-unknown")}!";

            case OutpostConsoleState.Captured:
                return $"{Loc.GetString("faction-captured-state-by")} {Loc.GetString("faction-unknown")}!";

            default:
                Sawmill.Error($"Unknown {console.Comp.State} state!");
                break;
        }

        return Loc.GetString("faction-default-string");
    }

    private string FormattedButton(Entity<OutpostConsoleComponent> console)
    {
        var faction = TryGetFactionInSlot(console, out var slot);
        if (slot.ContainedEntity == null)
            return $"{Loc.GetString("faction-insert-faction-id")}";

        if (faction == null)
            return $"{Loc.GetString("faction-id-card-not-belong-to-any-faction")}";

        return faction.ID == console.Comp.CapturedFaction
            ? $"{Loc.GetString("faction-cant-capture-already-captured-outpost")}"
            : $"{Loc.GetString("faction-start-capture-outpost")}";
    }
    #endregion

    private void ChangeCaptureState(Entity<OutpostConsoleComponent> console)
    {
        if (!CanChangeCaptureState(console, out var faction))
            return;

        var oldFaction = console.Comp.CapturedFaction;
        console.Comp.CapturedFaction = faction.ID;
        switch (console.Comp.State)
        {
            case OutpostConsoleState.Uncaptured:
                Sawmill.Info($"Faction {faction.ID}, start capturing console id - {console.Owner}");
                break;

            case OutpostConsoleState.Capturing:
                Sawmill.Info($"Faction {faction.ID}, intercepts capturing console faction, {oldFaction} on id - {console.Owner}");
                break;

            case OutpostConsoleState.Captured:
                Sawmill.Info($"Faction {faction.ID}, start recapturing console of faction, {oldFaction} on id - {console.Owner}");
                break;

            default:
                Sawmill.Error("Don't know how to change default capture state!");
                return;
        }
        string? oldFactionChannel = null;
        string? oldFactionName = null;
        if (oldFaction != null &&
            PrototypeManager.TryIndex<CharacterFactionPrototype>(oldFaction, out var oldFactionProto))
        {
            oldFactionChannel = oldFactionProto.RadioChannel;
            oldFactionName = oldFactionProto.Name;
        }

        StartCapture(console,
        faction.RadioChannel,
        oldFactionChannel,
        faction.ID,
        faction.Name,
        oldFactionName,
        console.Comp.State
        );
        console.Comp.State = OutpostConsoleState.Capturing;
        console.Comp.CapturedFactionName = faction.Name;
        Dirty(console);
    }

    private void StartCapture(Entity<OutpostConsoleComponent> console, string? newChannel, string? oldChannel,
        string? faction, string? factionName, string? oldFactionName, OutpostConsoleState state)
    {
        if (!TryGetEntity(console.Comp.LinkedOutpost, out var outpost) ||
        !TryComp<OutpostCaptureComponent>(outpost, out var outpostCapture) ||
        !TryGetNetEntity(console, out var netConsole))
        return;

        console.Comp.CapturingTime = console.Comp.CaptureTime;
        if (outpostCapture.CapturedConsoles.Contains(netConsole.Value))
            outpostCapture.CapturedConsoles.Remove(netConsole.Value);

        if (NetMan.IsServer)
        {
            var ev = new CaptureStartedEvent(newChannel, oldChannel, faction, factionName, oldFactionName, state);
            RaiseLocalEvent(console, ev);
        }

        if (outpostCapture.CapturingConsoles.Contains(netConsole.Value))
            return;

        outpostCapture.CapturingConsoles.Add(netConsole.Value);
    }

    private CharacterFactionPrototype? TryGetFactionInSlot(Entity<OutpostConsoleComponent> console, out ContainerSlot slot)
    {
        slot = ContainerSystem.EnsureContainer<ContainerSlot>(console,
            console.Comp.ContainerSlot,
            out var existed);

        if (!existed || slot.ContainedEntity == null)
            return null;

        var id = slot.ContainedEntity.Value;
        if (!TryComp<IdCardComponent>(id, out var card) || card.JobPrototype == null)
            return null;

        var prototype = card.JobPrototype.Value;
        if (!PrototypeManager.TryIndex(prototype, out var index))
            return null;

        var faction = index.ForceFaction;
        return !PrototypeManager.TryIndex(faction, out var factionIndex) ? null : factionIndex;
    }

    private bool CanChangeCaptureState(Entity<OutpostConsoleComponent> console,
        [NotNullWhen(true)] out CharacterFactionPrototype? faction)
    {
        faction = TryGetFactionInSlot(console, out _);
        if (faction == null)
            return false;

        if (console.Comp.LinkedOutpost == null)
            return false;

        return console.Comp.CapturedFaction != faction.ID;
    }
    #endregion

    public float? UpdateProgressBar(Entity<OutpostConsoleComponent> console)
    {
        var progress = (float?) (1f - console.Comp.CapturingTime / console.Comp.CaptureTime) * 100f;
        progress = progress != null ? float.Clamp(progress.Value, 0f, 100f) : null;
        return progress;
    }
}

[Prototype("outpostSpawn")]
// ReSharper disable once PartialTypeWithSinglePart Ридер глупи.
public sealed partial class OutpostSpawnPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = null!;

    [DataField("spawn")]
    public List<EntitySpawnEntry> SpawnList = [];
}

#region Serializable
[Serializable, NetSerializable]
public sealed class OutpostUIState(float? progress, bool disabled, string labelState, string buttonState) : BoundUserInterfaceState
{
    public float? Progress => progress;
    public bool Disabled => disabled;
    public string LabelState => labelState;
    public string ButtonState => buttonState;
}

[Serializable, NetSerializable]
public sealed class OutpostCaptureButtonPressed : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ProgressBarUpdate(float? value) : BoundUserInterfaceMessage
{
    public float? Value = value;
}

[Serializable, NetSerializable]
public sealed class CaptureStartedEvent(
    string? radioChannel,
    string? oldRadioChannel,
    string? faction,
    string? factionName,
    string? oldFactionName,
    OutpostConsoleState state) : EntityEventArgs
{
    public string? RadioChannel => radioChannel;
    public string? OldRadioChannel => oldRadioChannel;
    public string? Faction => faction;
    public string? FactionName => factionName;
    public string? OldFactionName => oldFactionName;
    public OutpostConsoleState State => state;
}

[Serializable, NetSerializable]
public enum CaptureUIKey : byte
{
    Key = 0,
}
#endregion
