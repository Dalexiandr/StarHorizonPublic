using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Horizon.CursedKatana;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CursedKatanaComponent : Component
{
    [DataField("ownerUid")]
    public EntityUid OwnerUid;

    [DataField("isActive")]
    public bool IsActive { get; set; } = false;

    [DataField("ownerIdentified")]
    public bool OwnerIdentified { get; set; } = false;

    [DataField("originalWalkSpeed")]
    public float OriginalWalkSpeed { get; set; }

    [DataField("originalSprintSpeed")]
    public float OriginalSprintSpeed { get; set; }

    [DataField("speedReduced")]
    public bool SpeedReduced { get; set; } = false;

    [DataField("damageTimer")]
    public float DamageTimer { get; set; } = 1.0f;

    [DataField("damageInterval")]
    public float DamageInterval { get; set; } = 1.0f;

    [DataField("originalDamage")]
    public DamageSpecifier OriginalDamage = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activateCursedKatanaAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActivateCursedKatanaAction = "ActionActivateCursedKatana";

    [DataField, AutoNetworkedField]
    public EntityUid? ActivateCursedKatanaActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deactivateCursedKatanaAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DeactivateCursedKatanaAction = "ActionDeactivateCursedKatana";

    [DataField, AutoNetworkedField]
    public EntityUid? DeactivateCursedKatanaActionEntity;

    [DataField("oneBlockMessage")]
    public List<string> OneBlockMessage { get; set; } = new()
    {
        "Когда-то я гнался за непокорным ветром. Теперь я охочусь на бушующую тьму.",
        "Меня путают с кошмарами. Хех…",
        "Во второй жизни я уже проклят...",
        "Я думал, что смерть принесёт мне покой. Как же я был наивен…",
        "Меня терзает прошлое, которое я не в силах изменить...",
        "Обрету ли я когда-нибудь покой?...",
        "Ммм… Охота продолжается.",
        "Ах… Как я соскучился по еде.",
        "Демоны принимают разные формы, но для охоты это не важно.",
        "Теперь вся моя жизнь – это охота.",
        "Я здесь не для того, чтобы спасать души.",
        "Если назвать страх по имени, он потеряет власть.",
        "Разрушить жизнь легко. Я это знаю.",
        "Люди лгут. Мечи – нет.",
        "Моргни – и пропустишь свою смерть.",
        "Твоя смерть не принесёт мне радости.",
        "Два пути пересеклись, один завершился.",
        "Жаль, что ты не видишь истинную угрозу.",
        "Смерть отвергает меня, но с радостью примет тебя."
    };

    [DataField("twoBlockMessage")]
    public List<string> TwoBlockMessage { get; set; } = new()
    {
        "Воплощение кошмаров, я прикончу тебя раньше, чем ты доберёшься до застенчивой лани.",
        "Доброта – непозволительная роскошь.",
        "Это твоё лекарство.",
        "С дороги!",
        "Я спасу тебя от тебя.",
        "Отступись!",
        "Страх – твои оковы.",
        "Жертва неуверенности!",
        "Почему ты отвергаешь искупление?",
        "Тебе уже не помочь.",
        "Сомнения выдают тебя.",
        "От страха не сбежать.",
        "Жаль клинки о тебя марать.",
        "Сожаления – твоя слабость!",
        "Внутренние демоны… Хм.",
        "Без жалости и сожалений.",
        "Похоже, ты во власти демона.",
        "Отступи, или падешь от моих клинков.",
        "Настоящая битва в твоей душе.",
        "Оса-и!",
        "Ко-ан!",
        "Ка-се-тон!",
        "Разрубить!",
        "Прятаться негде!",
        "Сквозь пелену!",
        "Тысяча порезов!",
        "Сгинь!",
        "Прощай…"
    };

    [DataField("threeBlockMessage")]
    public List<string> ThreeBlockMessage { get; set; } = new()
    {
        "Пора заканчивать.",
        "Я отступаю, чтобы снова идти вперед",
        "Ещё один демон отправляется туда, где ему и место.",
        "С каждой новой печатью демонов становится только больше.",
        "Я не враг тебе.",
        "Отбрось груз прошлого.",
        "Треснувшая маска и меч, утративший честь.",
        "Тобой не движет демон, нет смысла тратить на тебя время."
    };
}
