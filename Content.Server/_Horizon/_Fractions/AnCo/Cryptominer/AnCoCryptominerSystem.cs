using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Horizon._Fractions.AnCo.Cryptominer;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Horizon._Fractions.AnCo.Cryptominer;

public sealed class AnCoCryptominerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    private float _updateTimer;
    private const float UpdateTime = 1.0f; // Status/temperature update interval
    private const float CreditGenerationInterval = 60.0f; // Credits are generated once per minute

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoCryptominerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AnCoCryptominerComponent, CryptominerToggleMessage>(OnToggle);
        SubscribeLocalEvent<AnCoCryptominerComponent, EntInsertedIntoContainerMessage>(OnDiskInserted);
        SubscribeLocalEvent<AnCoCryptominerComponent, EntRemovedFromContainerMessage>(OnDiskRemoved);
    }

    private void OnDiskInserted(EntityUid uid, AnCoCryptominerComponent miner, EntInsertedIntoContainerMessage args)
    {
        UpdateDiskCount(uid, miner);
    }

    private void OnDiskRemoved(EntityUid uid, AnCoCryptominerComponent miner, EntRemovedFromContainerMessage args)
    {
        UpdateDiskCount(uid, miner);
    }

    private void UpdateDiskCount(EntityUid uid, AnCoCryptominerComponent miner)
    {
        var count = 0;

        if (_itemSlots.GetItemOrNull(uid, AnCoCryptominerComponent.DiskSlot1) != null)
            count++;
        if (_itemSlots.GetItemOrNull(uid, AnCoCryptominerComponent.DiskSlot2) != null)
            count++;
        if (_itemSlots.GetItemOrNull(uid, AnCoCryptominerComponent.DiskSlot3) != null)
            count++;
        if (_itemSlots.GetItemOrNull(uid, AnCoCryptominerComponent.DiskSlot4) != null)
            count++;

        miner.DiskCount = count;
        _appearance.SetData(uid, CryptominerVisuals.DiskCount, count);
        Dirty(uid, miner);
    }

    private void OnPowerChanged(EntityUid uid, AnCoCryptominerComponent miner, ref PowerChangedEvent args)
    {
        if (!args.Powered && miner.State != CryptominerState.Off)
        {
            miner.State = CryptominerState.Off;
            UpdateAppearance(uid, miner);
        }

        Dirty(uid, miner);
        UpdateUI(uid, miner);
    }

    private void OnToggle(EntityUid uid, AnCoCryptominerComponent miner, CryptominerToggleMessage args)
    {
        if (!_power.IsPowered(uid))
            return;

        if (miner.State == CryptominerState.Off || miner.State == CryptominerState.NoAtmosphere || miner.State == CryptominerState.NoDisks)
        {
            // Check disks first - cannot operate without disks
            if (miner.DiskCount <= 0)
            {
                miner.State = CryptominerState.NoDisks;
            }
            else
            {
                // Check atmosphere before turning on
                var environment = _atmosphere.GetContainingMixture(uid, true);
                var pressure = environment?.Pressure ?? 0f;

                if (pressure < miner.MinimumPressure)
                {
                    miner.State = CryptominerState.NoAtmosphere;
                }
                else
                {
                    miner.State = CryptominerState.Normal;
                }
            }
        }
        else
        {
            miner.State = CryptominerState.Off;
        }

        Dirty(uid, miner);
        UpdateAppearance(uid, miner);
        UpdateUI(uid, miner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer < UpdateTime)
            return;

        _updateTimer -= UpdateTime;

        var query = EntityQueryEnumerator<AnCoCryptominerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var miner, out var xform))
        {
            var isPowered = _power.IsPowered(uid);

            // Get current atmosphere
            var environment = _atmosphere.GetContainingMixture(uid, true);
            if (environment != null)
            {
                miner.CurrentTemperature = environment.Temperature;
                miner.CurrentPressure = environment.Pressure;
            }
            else
            {
                miner.CurrentPressure = 0f;
                // In vacuum temperature is undefined; reset so UI does not show stale value
                miner.CurrentTemperature = 0f;
            }

            // Check for no disks - cannot operate without disks
            if (miner.DiskCount <= 0 && miner.State != CryptominerState.Off)
            {
                miner.State = CryptominerState.NoDisks;
                miner.Efficiency = 0f;
                Dirty(uid, miner);
                UpdateAppearance(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // Check for low atmosphere - cannot operate in vacuum
            if (miner.CurrentPressure < miner.MinimumPressure && miner.State != CryptominerState.Off)
            {
                miner.State = CryptominerState.NoAtmosphere;
                miner.Efficiency = 0f;
                Dirty(uid, miner);
                UpdateAppearance(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // If not powered or off, skip processing
            if (!isPowered || miner.State == CryptominerState.Off)
            {
                if (miner.State != CryptominerState.Off && !isPowered)
                {
                    miner.State = CryptominerState.Off;
                    UpdateAppearance(uid, miner);
                }

                Dirty(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // Update state based on temperature
            var previousState = miner.State;
            UpdateTemperatureState(uid, miner);

            // If critical, the miner shuts down
            if (miner.State == CryptominerState.Critical)
            {
                miner.Efficiency = 0f;
                Dirty(uid, miner);
                UpdateAppearance(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // Calculate efficiency based on temperature
            CalculateEfficiency(miner);

            // Add heat to the environment (scales linearly with disk count)
            if (environment != null)
            {
                var heatToAdd = miner.BaseHeatEnergyPerSecond * miner.DiskCount * UpdateTime;
                _atmosphere.AddHeat(environment, heatToAdd);
            }

            // Accumulate time for credit generation
            miner.CreditGenerationTimer += UpdateTime;

            // Generate credits once per minute (scales linearly with disk count: 1 disk = 50, 4 disks = 200)
            if (miner.CreditGenerationTimer >= CreditGenerationInterval)
            {
                miner.CreditGenerationTimer -= CreditGenerationInterval;

                var creditsGenerated = (int)(miner.BaseCreditsPerMinute * miner.DiskCount * miner.Efficiency);

                if (creditsGenerated > 0)
                {
                    // Add credits to disks
                    AddCreditsToDisks(uid, creditsGenerated);
                }
            }

            if (previousState != miner.State)
            {
                UpdateAppearance(uid, miner);
            }

            Dirty(uid, miner);
            UpdateUI(uid, miner);
        }
    }

    private void UpdateTemperatureState(EntityUid uid, AnCoCryptominerComponent miner)
    {
        if (miner.CurrentTemperature >= miner.CriticalTemperature)
        {
            miner.State = CryptominerState.Critical;
        }
        else if (miner.CurrentTemperature >= miner.OverheatTemperature)
        {
            miner.State = CryptominerState.Overheat;
        }
        else if (miner.CurrentTemperature >= miner.WarningTemperature)
        {
            miner.State = CryptominerState.Warning;
        }
        else
        {
            miner.State = CryptominerState.Normal;
        }
    }

    private void CalculateEfficiency(AnCoCryptominerComponent miner)
    {
        if (miner.CurrentTemperature < miner.WarningTemperature)
        {
            miner.Efficiency = 1.0f;
        }
        else if (miner.CurrentTemperature < miner.OverheatTemperature)
        {
            // Linear interpolation from 100% to 75% efficiency
            var t = (miner.CurrentTemperature - miner.WarningTemperature) /
                    (miner.OverheatTemperature - miner.WarningTemperature);
            miner.Efficiency = 1.0f - (t * 0.25f);
        }
        else if (miner.CurrentTemperature < miner.CriticalTemperature)
        {
            // Linear interpolation from 75% to 25% efficiency
            var t = (miner.CurrentTemperature - miner.OverheatTemperature) /
                    (miner.CriticalTemperature - miner.OverheatTemperature);
            miner.Efficiency = 0.75f - (t * 0.5f);
        }
        else
        {
            miner.Efficiency = 0f;
        }
    }

    /// <summary>
    /// Distributes credits evenly across all inserted disks.
    /// Returns the number of credits actually stored.
    /// </summary>
    private int AddCreditsToDisks(EntityUid minerUid, int amount)
    {
        var disks = new List<(EntityUid Uid, CryptominerDiskComponent Disk)>();

        // Collect all disks with CryptominerDiskComponent
        var disk1 = _itemSlots.GetItemOrNull(minerUid, AnCoCryptominerComponent.DiskSlot1);
        var disk2 = _itemSlots.GetItemOrNull(minerUid, AnCoCryptominerComponent.DiskSlot2);
        var disk3 = _itemSlots.GetItemOrNull(minerUid, AnCoCryptominerComponent.DiskSlot3);
        var disk4 = _itemSlots.GetItemOrNull(minerUid, AnCoCryptominerComponent.DiskSlot4);

        if (disk1 != null && TryComp<CryptominerDiskComponent>(disk1, out var diskComp1))
            disks.Add((disk1.Value, diskComp1));
        if (disk2 != null && TryComp<CryptominerDiskComponent>(disk2, out var diskComp2))
            disks.Add((disk2.Value, diskComp2));
        if (disk3 != null && TryComp<CryptominerDiskComponent>(disk3, out var diskComp3))
            disks.Add((disk3.Value, diskComp3));
        if (disk4 != null && TryComp<CryptominerDiskComponent>(disk4, out var diskComp4))
            disks.Add((disk4.Value, diskComp4));

        if (disks.Count == 0)
            return 0;

        var totalStored = 0;
        var creditsPerDisk = amount / disks.Count;
        var remainder = amount % disks.Count;

        foreach (var (diskUid, disk) in disks)
        {
            var toAdd = creditsPerDisk + (remainder > 0 ? 1 : 0);
            if (remainder > 0)
                remainder--;

            var availableSpace = disk.MaxCredits - disk.StoredCredits;
            var actualAdd = Math.Min(toAdd, availableSpace);

            disk.StoredCredits += actualAdd;
            totalStored += actualAdd;
            Dirty(diskUid, disk);
        }

        return totalStored;
    }

    private void UpdateAppearance(EntityUid uid, AnCoCryptominerComponent miner)
    {
        _appearance.SetData(uid, CryptominerVisuals.State, miner.State);
    }

    private void UpdateUI(EntityUid uid, AnCoCryptominerComponent miner)
    {
        var isPowered = _power.IsPowered(uid);

        // Effective credits per minute (base rate, before efficiency)
        var effectiveCreditsPerMinute = miner.BaseCreditsPerMinute * miner.DiskCount;

        var state = new CryptominerBoundUserInterfaceState(
            miner.State,
            miner.CurrentTemperature,
            miner.WarningTemperature,
            miner.OverheatTemperature,
            miner.CriticalTemperature,
            effectiveCreditsPerMinute,
            miner.Efficiency,
            miner.PowerConsumption,
            isPowered,
            miner.DiskCount
        );

        _ui.SetUiState(uid, CryptominerUiKey.Key, state);
    }
}
