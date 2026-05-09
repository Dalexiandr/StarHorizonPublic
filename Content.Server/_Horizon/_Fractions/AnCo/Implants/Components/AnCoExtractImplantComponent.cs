using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server._Horizon._Fractions.AnCo.Implants.Components;

/// <summary>
/// Имплант помещающий в мешок для трупов и телепортации если он на экспедиции
/// </summary>
[RegisterComponent]
public sealed partial class AnCoExtractImplantComponent : Component
{

    /// <summary>
    /// Упаковывать ли в мешок
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool PackBody = true;

    /// <summary>
    /// Убивать ли игрока если активирова
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool KillOnActivate = true;

    /// <summary>
    /// Телепортировать ли тело которое не на экспедиции
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool TeleportAnywhere = false;

    /// <summary>
    /// Телепорт когда экспа вот-вот закончится
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireExpeditionFinalStage = true;

    /// <summary>
    /// Кд перед телепортом
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportDelay = 5f;

    /// <summary>
    /// Прототип мешка в который будет запихан игрок
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string BodyBagPrototype = "BodyBag";

    /// <summary>
    /// Наносит урон после активации
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? ActivationDamage = new()
    {
        DamageDict = new()
        {
            { "Cellular", 10 }
        }
    };

    /// <summary>
    /// Звук запихивание игрока в мешок
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// Эффект который используется на месте телепорта мешка
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string TeleportEffect = "BluespaceLifelineSpawnCore";

    /// <summary>
    /// Поиск ближайших телепадов по id
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? TelepadPrototypeId;

    public bool Activated => BodyBag != null;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BodyBag;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? TeleportTime;

}
