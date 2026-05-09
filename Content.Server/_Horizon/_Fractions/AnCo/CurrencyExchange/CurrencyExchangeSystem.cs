using Content.Server.Stack;
using Content.Shared._Horizon._Fractions.AnCo.CurrencyExchange;
using Content.Shared._Horizon._Fractions.AnCo.Cryptominer;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._Horizon._Fractions.AnCo.CurrencyExchange;

public sealed class CurrencyExchangeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    private float _globalUpdateTimer;
    private const float GlobalUpdateInterval = 60f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrencyExchangeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CurrencyExchangeComponent, EntInsertedIntoContainerMessage>(OnDiskInserted);
        SubscribeLocalEvent<CurrencyExchangeComponent, EntRemovedFromContainerMessage>(OnDiskRemoved);
        SubscribeLocalEvent<CurrencyExchangeComponent, CurrencyExchangeMessage>(OnExchange);
        SubscribeLocalEvent<CurrencyExchangeComponent, CurrencyExchangeAllMessage>(OnExchangeAll);
    }

    private void OnInit(EntityUid uid, CurrencyExchangeComponent component, ComponentInit args)
    {
        component.CurrentExchangeRate = component.BaseExchangeRate;
    }

    private void OnDiskInserted(EntityUid uid, CurrencyExchangeComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(uid, component);
        UpdateUI(uid, component);
    }

    private void OnDiskRemoved(EntityUid uid, CurrencyExchangeComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateAppearance(uid, component);
        UpdateUI(uid, component);
    }

    private void OnExchange(EntityUid uid, CurrencyExchangeComponent component, CurrencyExchangeMessage args)
    {
        var disk = _itemSlots.GetItemOrNull(uid, CurrencyExchangeComponent.DiskSlot);
        if (disk == null || !TryComp<CryptominerDiskComponent>(disk, out var diskComp))
            return;

        if (args.Amount <= 0 || args.Amount > diskComp.StoredCredits)
            return;

        // Calculate exchange with commission
        var grossCredits = (int)(args.Amount * component.CurrentExchangeRate);
        var commission = (int)(grossCredits * component.Commission);
        var netCredits = grossCredits - commission;

        if (netCredits <= 0)
            return;

        // Remove from disk
        diskComp.StoredCredits -= args.Amount;
        Dirty(disk.Value, diskComp);

        // Spawn credits under the terminal
        SpawnCredits(uid, netCredits);

        // Play exchange sound
        _audio.PlayPvs("/Audio/Machines/printer.ogg", uid);

        UpdateUI(uid, component);
    }

    private void OnExchangeAll(EntityUid uid, CurrencyExchangeComponent component, CurrencyExchangeAllMessage args)
    {
        var disk = _itemSlots.GetItemOrNull(uid, CurrencyExchangeComponent.DiskSlot);
        if (disk == null || !TryComp<CryptominerDiskComponent>(disk, out var diskComp))
            return;

        if (diskComp.StoredCredits <= 0)
            return;

        // Calculate exchange with commission
        var grossCredits = (int)(diskComp.StoredCredits * component.CurrentExchangeRate);
        var commission = (int)(grossCredits * component.Commission);
        var netCredits = grossCredits - commission;

        if (netCredits <= 0)
            return;

        // Remove all from disk
        diskComp.StoredCredits = 0;
        Dirty(disk.Value, diskComp);

        // Spawn credits under the terminal
        SpawnCredits(uid, netCredits);

        // Play exchange sound
        _audio.PlayPvs("/Audio/Machines/printer.ogg", uid);

        UpdateUI(uid, component);
    }

    private void SpawnCredits(EntityUid terminalUid, int amount)
    {
        if (amount <= 0)
            return;

        var coordinates = Transform(terminalUid).Coordinates;
        _stack.Spawn(amount, "Credit", coordinates);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _globalUpdateTimer += frameTime;

        if (_globalUpdateTimer < GlobalUpdateInterval)
            return;

        _globalUpdateTimer -= GlobalUpdateInterval;

        // Update all exchange rates
        var query = EntityQueryEnumerator<CurrencyExchangeComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateExchangeRate(uid, component);
            UpdateUI(uid, component);
        }
    }

    private void UpdateExchangeRate(EntityUid uid, CurrencyExchangeComponent component)
    {
        var oldRate = component.CurrentExchangeRate;

        // Random walk with trend influence
        var trendBias = component.RateTrend * 0.02f;
        var change = (_random.NextFloat() - 0.5f + trendBias) * component.RateVolatility * 2f;

        component.CurrentExchangeRate = Math.Clamp(
            component.CurrentExchangeRate + change,
            component.MinExchangeRate,
            component.MaxExchangeRate
        );

        // Update trend based on recent movement
        if (component.CurrentExchangeRate > oldRate)
            component.RateTrend = Math.Min(component.RateTrend + 1, 3);
        else if (component.CurrentExchangeRate < oldRate)
            component.RateTrend = Math.Max(component.RateTrend - 1, -3);

        // Add occasional trend reversal
        if (_random.Prob(0.1f))
            component.RateTrend = -component.RateTrend;

        Dirty(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, CurrencyExchangeComponent component)
    {
        var hasDisk = _itemSlots.GetItemOrNull(uid, CurrencyExchangeComponent.DiskSlot) != null;
        _appearance.SetData(uid, CurrencyExchangeVisuals.HasDisk, hasDisk);
    }

    private void UpdateUI(EntityUid uid, CurrencyExchangeComponent component)
    {
        var disk = _itemSlots.GetItemOrNull(uid, CurrencyExchangeComponent.DiskSlot);
        var hasDisk = disk != null;
        var diskCredits = 0;
        var diskMaxCredits = 0;

        if (hasDisk && TryComp<CryptominerDiskComponent>(disk, out var diskComp))
        {
            diskCredits = diskComp.StoredCredits;
            diskMaxCredits = diskComp.MaxCredits;
        }

        var state = new CurrencyExchangeBoundUserInterfaceState(
            component.CurrentExchangeRate,
            component.Commission,
            component.RateTrend,
            diskCredits,
            diskMaxCredits,
            hasDisk
        );

        _ui.SetUiState(uid, CurrencyExchangeUiKey.Key, state);
    }
}
