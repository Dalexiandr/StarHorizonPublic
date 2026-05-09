using System.Linq;
using Content.Server._Horizon.Planet;
using Content.Server._NF.Cargo.Systems;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Systems;
using Content.Server.CartridgeLoader;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._Horizon.Expeditions;
using Content.Shared.Cargo;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Expeditions;

public sealed class ExpeditionGoalsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PlanetSystem _planet = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private Dictionary<ProtoId<ExpeditionGoalCategoryPrototype>, Dictionary<int, ExpeditionGoal>> _goals = new();
    private Dictionary<int, ExpeditionGoal> _claimedGoals = new();
    private int _nextId = 1;
    private TimeSpan _nextOffer;

    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Количество целей на категорию
    /// </summary>
    public const int GoalsCount = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, ClaimExpeditionGoalMessage>(OnClaim);

        SubscribeLocalEvent<GoalsListCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<GoalsListCartridgeComponent, CartridgeUiMessage>(OnCartridgeMessage);

        SubscribeLocalEvent<SpawnExpeditionGoalEntityEvent>(OnSpawnEntities);
        SubscribeLocalEvent<PriceCalculationEvent>(GetPrice);
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
        SubscribeLocalEvent<NFEntitySoldEvent>(OnSold);
    }

    private void OnConsoleInit(Entity<ExpeditionGoalsConsoleComponent> ent, ref MapInitEvent args)
    {
        UpdateUi(ent.Owner);
    }

    private void OnClaim(Entity<ExpeditionGoalsConsoleComponent> ent, ref ClaimExpeditionGoalMessage args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard))
            return;

        if (!TryClaimGoal(idCard.Owner, args.OptionId, args.Specification))
        {
            _audio.PlayPvs(_audio.ResolveSound(new SoundCollectionSpecifier("CargoError")), ent.Owner);
            _popup.PopupEntity(Loc.GetString("exp-goal-cannot-claim"), ent.Owner, args.Actor);
        }
        else
        {
            _audio.PlayPvs(_audio.ResolveSound(new SoundPathSpecifier("/Audio/Items/appraiser.ogg")), ent.Owner);
            _popup.PopupEntity(Loc.GetString("exp-goal-claimed"), ent.Owner, args.Actor);
        }
    }

    private void OnUiReady(Entity<GoalsListCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        Dictionary<int, ExpeditionGoal> goals = new();

        // Проклятое получение всех целей с КПК
        if (TryComp<PdaComponent>(args.Loader, out var pda) &&
            pda.IdSlot?.ContainerSlot?.ContainedEntity is { Valid: true } card &&
            TryComp<ExpeditionGoalsIdCardComponent>(card, out var goalCard))
            goals = goalCard.AssignedGoals.Select(x => new KeyValuePair<int, ExpeditionGoal>(x, _claimedGoals[x])).ToDictionary();

        var state = new GoalsListCartridgeUiState(goals);
        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, state);
    }

    private void OnCartridgeMessage(Entity<GoalsListCartridgeComponent> ent, ref CartridgeUiMessage args)
    {
        if (args.MessageEvent is not GoalsListRemoveMessage cast)
            return;

        _claimedGoals.Remove(cast.Id);
        _cartridgeLoader.UpdateUiState(GetEntity(args.MessageEvent.LoaderUid), null, null);
    }


    private void OnSpawnEntities(SpawnExpeditionGoalEntityEvent args)
    {
        if (!_planet.LoadedPlanets.TryGetValue(args.Planet, out var planetUid))
        {
            Log.Warning("Tried to spawn expedition goal target on non-exsisting planet.");
            return;
        }

        var markers = EntityManager.AllEntities<TagComponent>().Where(x => _tag.HasTag(x.Owner, args.SpawnerTag) && Transform(x).Coordinates.EntityId == planetUid).ToList();
        _random.Shuffle(markers);

        if (markers.Count <= 0)
        {
            Log.Warning("Tried to spawn expedition goal target without having markers.");
            return;
        }


        for (var i = 0; i < markers.Count && i < args.MarkersCount; i++)
        {
            var markerCoords = Transform(markers[i]).Coordinates;

            for (var e = 0; e < args.SpawnsPerMarker; e++)
            {
                var ent = Spawn(_random.Pick(args.SpawnedEntities), markerCoords);
            }
        }
    }

    private void GetPrice(ref PriceCalculationEvent args)
    {
        // Нам нужен юзер для проверки по КПК
        if (!args.User.HasValue)
            return;

        if (!_idCard.TryFindIdCard(args.User.Value, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return;

        foreach (var item in goalsCard.AssignedGoals)
        {
            if (!_claimedGoals.TryGetValue(item, out var goal))
                continue;

            if (!goal.TryComplete(args.Entity, EntityManager))
                continue;

            // Проверяем, соответствует ли валюта
            if (args.Currency != goal.RequiredStack)
                continue;

            // Контрабандные бонусы получаются отдельно
            if (goal.IsContraband)
                continue;

            args.Price = goal.Reward;
            args.Handled = true;
            return;
        }
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return;

        foreach (var sold in args.Sold)
        {
            foreach (var item in goalsCard.AssignedGoals.ToList())
            {
                if (!_claimedGoals.TryGetValue(item, out var goal))
                    continue;

                // Если цель контрабандная, на обычной консоли она не выполнится
                if (goal.IsContraband)
                    continue;

                if (!goal.TryComplete(sold, EntityManager))
                    continue;

                // Выдаём доп награду
                if (goal.RewardEntity != null)
                {
                    var ent = Spawn(goal.RewardEntity, Transform(args.Actor).Coordinates);
                    _hands.TryPickupAnyHand(args.Actor, ent);
                }

                goalsCard.AssignedGoals.Remove(item);
            }

            Dirty(idCard.Owner, goalsCard);

            // Обновление UI КПК
            if (_container.TryGetContainingContainer(idCard.Owner, out var container) && TryComp<CartridgeLoaderComponent>(container.Owner, out var loader))
                _cartridgeLoader.UpdateUiState(container.Owner, null, loader);
        }
    }

    private void OnSold(ref NFEntitySoldEvent args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return;

        foreach (var sold in args.Sold)
        {
            foreach (var item in goalsCard.AssignedGoals.ToList())
            {
                if (!_claimedGoals.TryGetValue(item, out var goal))
                    continue;

                if (!goal.TryComplete(sold, EntityManager))
                    continue;

                // Выдаём доп награду
                if (goal.RewardEntity != null)
                {
                    var ent = Spawn(goal.RewardEntity, Transform(args.Actor).Coordinates);
                    _hands.TryPickupAnyHand(args.Actor, ent);
                }

                goalsCard.AssignedGoals.Remove(item);
            }

            Dirty(idCard.Owner, goalsCard);

            // Обновление UI КПК
            if (_container.TryGetContainingContainer(idCard.Owner, out var container) && TryComp<CartridgeLoaderComponent>(container.Owner, out var loader))
                _cartridgeLoader.UpdateUiState(container.Owner, null, loader);
        }
    }

    /// <summary>
    /// Получает бонус для контрабандных целей
    /// </summary>
    /// <param name="actor"></param>
    /// <param name="ent"></param>
    /// <param name="currency"></param>
    /// <returns></returns>
    public int GetContrabandBonus(EntityUid actor, EntityUid ent, string currency)
    {
        if (!_idCard.TryFindIdCard(actor, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return 0;

        foreach (var item in goalsCard.AssignedGoals)
        {
            if (!_claimedGoals.TryGetValue(item, out var goal))
                continue;

            if (!goal.IsContraband)
                continue;

            if (goal.RequiredStack != currency)
                continue;

            if (goal.TryComplete(ent, EntityManager))
                return goal.Reward;
        }

        return 0;
    }

    /// <summary>
    /// Принимает цель с определённым айди
    /// </summary>
    /// <param name="idCard">Карта, к которой будет привязана цель</param>
    /// <param name="goalId">Айди цели</param>
    /// <param name="specification">Категория</param>
    private void ClaimGoal(EntityUid idCard, int goalId, ProtoId<ExpeditionGoalCategoryPrototype> specification)
    {
        if (!_goals[specification].TryGetValue(goalId, out var goal))
            return;

        if (goal.ClaimEvent != null)
            RaiseLocalEvent(goal.ClaimEvent);

        var card = EnsureComp<ExpeditionGoalsIdCardComponent>(idCard);
        card.AssignedGoals.Add(goalId);
        Dirty(idCard, card);

        _claimedGoals[goalId] = goal;

        GenerateGoals();
        UpdateUi();

        if (_container.TryGetContainingContainer(idCard, out var container))
            _cartridgeLoader.UpdateUiState(container.Owner, null, null);
    }

    /// <summary>
    /// Пытается принять цель с определённым id
    /// </summary>
    /// <param name="idCard">Карта, к которой будет привязана цель</param>
    /// <param name="goalId">Айди цели</param>
    /// <param name="specification">Категория</param>
    /// <returns>Принята цель, или нет</returns>
    private bool TryClaimGoal(EntityUid idCard, int goalId, ProtoId<ExpeditionGoalCategoryPrototype> specification)
    {
        if (!_goals[specification].TryGetValue(goalId, out var goal))
            return false;

        if (TryComp<ExpeditionGoalsIdCardComponent>(idCard, out var card) && card.AssignedGoals.Count >= card.MaxGoals)
            return false;

        ClaimGoal(idCard, goalId, specification);
        return true;
    }

    /// <summary>
    /// Выполняет ли указанная сущность какую-либо из целей
    /// </summary>
    /// <param name="user"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool IsCompleted(EntityUid user, EntityUid target)
    {
        if (!_idCard.TryFindIdCard(user, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return false;

        foreach (var item in goalsCard.AssignedGoals)
        {
            if (!_claimedGoals.TryGetValue(item, out var goal))
                continue;

            if (goal.TryComplete(target, EntityManager))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Генерирует новые цели, удаляя предыдущие
    /// </summary>
    private void GenerateGoals()
    {
        _goals.Clear();
        _nextOffer = _timing.CurTime + Cooldown;

        var prototypes = _proto.EnumeratePrototypes<ExpeditionGoalPrototype>().ToList();
        var categories = _proto.EnumeratePrototypes<ExpeditionGoalCategoryPrototype>().ToList();

        foreach (var item in categories)
        {
            _goals[item] = new();
            var specificated = prototypes.Where(x => x.Specification == item).ToList();
            if (specificated.Count <= 0)
                continue;

            for (var i = 0; i < GoalsCount; i++)
            {
                var proto = _random.Pick(specificated);
                var goal = proto.Goal.Instantiate(proto.RandomAmount.Next(_random) * proto.AmountMultiplier);

                // Добавляю отдельно сущность
                goal.RewardEntity = proto.RewardEntity;

                // Добавление цели в список
                _goals[item].Add(_nextId, goal);
                _nextId++;
            }
        }
    }

    /// <summary>
    /// Обновляет UI одной конкретной консоли
    /// </summary>
    private void UpdateUi(EntityUid uid)
    {
        if (!TryComp<ExpeditionGoalsConsoleComponent>(uid, out var console))
            return;

        _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
            new ExpeditionGoalsConsoleUiState(_goals, console.Categories, Cooldown, _nextOffer));
    }

    /// <summary>
    /// Обновляет UI всех консолей с целями
    /// </summary>
    private void UpdateUi()
    {
        var query = EntityQueryEnumerator<ExpeditionGoalsConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
                new ExpeditionGoalsConsoleUiState(_goals, console.Categories, Cooldown, _nextOffer));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextOffer)
            return;

        GenerateGoals();
        UpdateUi();
    }
}
