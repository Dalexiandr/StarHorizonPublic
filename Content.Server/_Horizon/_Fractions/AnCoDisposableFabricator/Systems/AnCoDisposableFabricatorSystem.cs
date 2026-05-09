using Content.Server._Horizon._Fractions.AnCoDisposableFabricator.Components;
using Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;
using Robust.Server.GameObjects;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Horizon._Fractions.AnCoDisposableFabricator.Systems;

/// <summary>
/// <see cref="AnCoDisposableFabricatorComponent"/>
/// This system links the interface to the logic. When approved, starts work animation,
/// then spawns items at the structure's position and deletes the structure.
/// </summary>
public sealed class AnCoDisposableFabricatorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoDisposableFabricatorComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<AnCoDisposableFabricatorComponent, AnCoDisposableFabricatorApproveMessage>(OnApprove);
        SubscribeLocalEvent<AnCoDisposableFabricatorComponent, AnCoDisposableFabricatorChangeSetMessage>(OnChangeSet);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnCoDisposableFabricatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsWorking || comp.WorkEndTime == null)
                continue;

            if (_timing.CurTime < comp.WorkEndTime)
                continue;

            FinishWork(uid, comp);
        }
    }

    private void OnUIOpened(Entity<AnCoDisposableFabricatorComponent> fabricator, ref BoundUIOpenedEvent args)
    {
        UpdateUI(fabricator.Owner, fabricator.Comp);
    }

    private void OnApprove(Entity<AnCoDisposableFabricatorComponent> fabricator, ref AnCoDisposableFabricatorApproveMessage args)
    {
        if (fabricator.Comp.SelectedSets.Count != fabricator.Comp.MaxSelectedSets)
            return;

        if (fabricator.Comp.IsWorking)
            return;

        StartWork(fabricator.Owner, fabricator.Comp);
        _ui.CloseUi(fabricator.Owner, AnCoDisposableFabricatorUIKey.Key);
    }

    private void StartWork(EntityUid uid, AnCoDisposableFabricatorComponent comp)
    {
        comp.IsWorking = true;
        comp.WorkEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.WorkDuration);

        _appearance.SetData(uid, AnCoDisposableFabricatorVisuals.IsWorking, true);
        _audio.PlayPvs(comp.WorkingSound, uid);
    }

    private void FinishWork(EntityUid uid, AnCoDisposableFabricatorComponent comp)
    {
        var coordinates = Transform(uid).Coordinates;

        foreach (var i in comp.SelectedSets)
        {
            var set = _proto.Index(comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                Spawn(item, coordinates);
            }
        }

        QueueDel(uid);
    }

    private void OnChangeSet(Entity<AnCoDisposableFabricatorComponent> fabricator, ref AnCoDisposableFabricatorChangeSetMessage args)
    {
        if (fabricator.Comp.IsWorking)
            return;

        if (!fabricator.Comp.SelectedSets.Remove(args.SetNumber))
            fabricator.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(fabricator.Owner, fabricator.Comp);
    }

    private void UpdateUI(EntityUid uid, AnCoDisposableFabricatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, AnCoDisposableFabricatorSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new AnCoDisposableFabricatorSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, AnCoDisposableFabricatorUIKey.Key, new AnCoDisposableFabricatorBoundUserInterfaceState(data, component.MaxSelectedSets));
    }
}
