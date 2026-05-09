using Content.Server.Mining.Components;
using Content.Server.Ore;
using Content.Shared.Examine;
using System.Linq;
using System.Text;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Ore;

public sealed class OreScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OreDepositComponent, ExaminedEvent>(OnDepositExamined);
    }

    /// <summary>
    /// Показывает что находится в жилах если сканер в руках
    /// </summary>
    private void OnDepositExamined(Entity<OreDepositComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var scannerLevel = GetPlayerHandScannerLevel(args.Examiner);
        if (scannerLevel == 0)
            return;

        var sortedOres = ent.Comp.OreCounts
            .OrderByDescending(x => x.Value)
            .ToList();

        if (sortedOres.Count == 0)
            return;

        var message = new StringBuilder();
        message.AppendLine("Сканер обнаружил что в жиле находится:");

        var oresToShow = scannerLevel >= 5 ? sortedOres.Count : Math.Min(scannerLevel, sortedOres.Count);

        for (int i = 0; i < oresToShow && i < sortedOres.Count; i++)
        {
            var (oreType, amount) = sortedOres[i];
            var displayName = GetOreDisplayName(oreType);
            message.AppendLine($"{displayName}: {amount}");
        }

        if (oresToShow < sortedOres.Count)
        {
            message.AppendLine("Неизвестно");
        }

        args.PushText(message.ToString());
    }

    private int GetPlayerHandScannerLevel(EntityUid player)
    {
        var item = _handsSystem.GetActiveItem(player);
        if (item == null || !TryComp<OreScannerComponent>(item, out var scanner))
            return 0;

        return scanner.ScanLevel;
    }

    private string GetOreDisplayName(string oreType)
    {
        return oreType switch
        {
            "SteelOre" => "Железная руда",
            "GoldOre" => "Золотоносная руда",
            "DiamondOre" => "Необработанные алмазы",
            "PlasmaOre" => "Плазменная руда",
            "UraniumOre" => "Урановая руда",
            "Coal" => "Уголь",
            "SilverOre" => "Серебряная руда",
            "SpaceQuartz" => "Космический кварц",
            "BananiumOre" => "Бананиумовая руда",
            "Salt" => "Соль",
            "PsycoreOre" => "Псикориевая руда",
            "TitanOre" => "Титановая руда",
            "EmeraldOre" => "Необработанные изумруды",
            "TopazOre" => "Необработанные топазы",
            "SapphireOre" => "Необработанные сапфиры",
            "RubyOre" => "Необработанные рубины",
            "ToriumOre" => "Ториевая руда",
            "PlutoniumOre" => "Плутониевая руда",
            "NeptuniumOre" => "Нептуниевая руда",
            _ => oreType
        };
    }
}
