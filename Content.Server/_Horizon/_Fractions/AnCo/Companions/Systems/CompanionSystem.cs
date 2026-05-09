using Content.Server._Horizon._Fractions.AnCo.Companions.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Horizon._Fractions.AnCo.Companions;
using Content.Shared.Access.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._Horizon._Fractions.AnCo.Companions.Systems;

/// <summary>
/// Система обрабатывающая компаньонов.
/// <para>
/// Отслеживает нажатие КПК/ID-карты по сущности и если сущность
/// имеет компонент <see cref="CompanionComponent"/>, то ему
/// присваивается EntityUid ID-карты.
/// </para>
/// </summary>
[Experimental("HORIZON_SYSTEM_01")]
public sealed class CompanionSystem : SharedCompanionSystem
{
    // Зависимости
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    // Временный флаг, помечающий, что система не активна.
    private bool _isActive = false;

    private readonly ISawmill _sawmill = Logger.GetSawmill("companions");

    public override void Initialize()
    {
        base.Initialize();

        if (_isActive)
        {
            SubscribeLocalEvent<CompanionComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<HandsComponent, AfterPointedAtEvent>(OnAfterPointedAt);
            SubscribeLocalEvent<CompanionCartridgeComponent, BoundUIOpenedEvent>(OnUiOpen);
            _sawmill.Info("Система компаньонов (CompanionSystem) активна!");
        }
        else
        {
            _sawmill.Info("Система компаньонов (CompanionSystem) неактивна...");
        }
    }

    private void OnInteractUsing(EntityUid uid, CompanionComponent comp, InteractUsingEvent args)
    {
        // Отмена привязки через импульсы
        if (TryComp<EmagComponent>(args.Used, out var emag) && TryComp<WiresPanelComponent>(uid, out var panelState))
        {
            if (comp.IdCard.Id == 0 || !panelState.Open)
                return;

            if (_sharedCharges.IsEmpty(args.Used))
                return;

            _popup.PopupPredicted($"Карточка замыкает что-то в {Identity.Entity(uid, EntityManager)}.", uid, uid, PopupType.Medium);
            _audio.PlayPredicted(emag.EmagSound, args.Used, args.Used);

            _sharedCharges.TryUseCharge(args.Used);

            comp.IdCard = default;
            args.Handled = true;
            return;
        }

        // Привязка сущности компаньона к ID-карте или через КПК.
        if (TryComp<PdaComponent>(args.Used, out var pdaComp))
        {
            if (comp.IdCard == pdaComp.ContainedId)
            {
                comp.IdCard = default;
                if (TryComp<HTNComponent>(args.User, out var htnComp))
                    _htn.Replan(htnComp);
                _popup.PopupEntity("Киборг удалил данные КПК из памяти.", uid, args.User, PopupType.Medium);
                return;
            }

            if (comp.IdCard.Id != 0 && comp.IdCard != pdaComp.ContainedId)
            {
                _popup.PopupEntity("Киборг отказался сканировать КПК.", uid, args.User);
                return;
            }

            if (comp.IdCard.Id == 0 && pdaComp.ContainedId != null)
            {
                _popup.PopupEntity("Киборг сканирует КПК и записывает данные в память.", uid, args.User);
                comp.IdCard = (EntityUid)pdaComp.ContainedId;
                return;
            }

            if (comp.IdCard.Id == 0 && pdaComp.ContainedId == null)
            {
                _popup.PopupEntity("Киборг не нашёл ID-карту в КПК.", uid, args.User);
                return;
            }
        }

        // Проверка на компонент ID карты
        if (TryComp<IdCardComponent>(args.Used, out var idCardComp))
        {
            if (comp.IdCard == args.Used)
            {
                comp.IdCard = default;
                if (TryComp<HTNComponent>(args.User, out var htnComp))
                    _htn.Replan(htnComp);
                _popup.PopupEntity("Данные ID-карты удалены из памяти.", uid, args.User, PopupType.Medium);
                return;
            }

            if (comp.IdCard.Id != 0 && comp.IdCard != args.Used)
            {
                _popup.PopupEntity("Невозможно перезаписать данные ID-карты.", uid, args.User);
                return;
            }

            if (comp.IdCard.Id == 0)
            {
                _popup.PopupEntity("ID-карта записана в память.", uid, args.User);
                comp.IdCard = args.Used;
                return;
            }
        }

        args.Handled = true;
    }

    /// <summary>
    /// Слушатель отвечающий за получение данных на указываемый предмет.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <param name="args"></param>
    private void OnAfterPointedAt(EntityUid uid, HandsComponent comp, AfterPointedAtEvent args)
    {
        var player = uid;
        var target = args.Pointed;

        var query = EntityQueryEnumerator<CompanionComponent, HTNComponent>();
        while (query.MoveNext(out var companionUid, out var compComp, out var htn))
        {
            if (compComp.IdCard.Id == 0 || !IsIdOwned(uid, compComp.IdCard))
                continue;

            ExecuteCompanionCommand(player, companionUid, htn, args.Pointed);
        }
    }

    /// <summary>
    /// Проверка на владельца Id карты.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="idCard"></param>
    /// <returns></returns>
    private bool IsIdOwned(EntityUid player, EntityUid idCard)
    {
        foreach (EntityUid item in _hands.EnumerateHeld(player))
        {
            if (item == idCard)
                return true;

            if (HasComp<PdaComponent>(item))
            {
                if (_container.TryGetContainer(item, "id_slot", out var container)
                    && container.ContainedEntities.Contains(idCard))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Метод для обработки поведения компаньонов в зависимости от отданной команды.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="companion"></param>
    /// <param name="htnCompanion"></param>
    /// <param name="target"></param>
    [Obsolete("Для более продвинутой системы комманд будет создана отдельно система отдачи команд.")]
    private void ExecuteCompanionCommand(EntityUid player, EntityUid companion, HTNComponent htnCompanion, EntityUid target)
    {
        if (target == player)
        {
            htnCompanion.Blackboard.SetValue("ModeInfinite", true);
            _popup.PopupEntity("Киборг начал бесконечно следовать за вами.", companion);
        }
        else
        {
            htnCompanion.Blackboard.SetValue("ModeOnce", true);
            _popup.PopupEntity("Киборг зафиксировал цель.", companion);
        }

        htnCompanion.Blackboard.SetValue("FollowTarget", Transform(target).Coordinates);
        htnCompanion.Blackboard.SetValue("FollowRange", 1.8f);
        htnCompanion.Blackboard.SetValue("FollowCloseRange", 0.85f);

        _htn.Replan(htnCompanion);
    }

    #region Companion UI функции
    /*
     * Логика обработки интерфейсов для КПК связанные с компаньонами.
     */

    private void OnUiOpen(EntityUid uid, CompanionCartridgeComponent cartridge, BoundUIOpenedEvent args)
    {
        _sawmill.Debug($"Игрок {ToPrettyString(uid)} открыл интерфейс компаньонов.");
        if (args.UiKey.Equals(CompanionUiKey.Key))
            UpdatePDAInterface(uid);
    }

    private void UpdatePDAInterface(EntityUid pda)
    {
        if (!_inventory.TryGetSlotEntity(pda, "id", out var idCard))
            return;

        var companions = new List<CompanionEntry>();
        var query = EntityQueryEnumerator<CompanionComponent, HTNComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var companion, out var htn, out var meta))
        {
            if (companion.IdCard == idCard)
            {
                var status = htn.Plan != null ? "В ожидании" : "Ожидание команды";
                companions.Add(new CompanionEntry(GetNetEntity(uid), meta.EntityName, status, 100f));
            }
            _userInterface.SetUiState(pda, CompanionUiKey.Key, new CompanionPDABoundUserInterfaceState(companions));
        }
    }

    #endregion
}
