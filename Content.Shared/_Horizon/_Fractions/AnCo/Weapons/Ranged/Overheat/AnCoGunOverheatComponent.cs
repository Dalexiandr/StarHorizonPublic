using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon._Fractions.AnCo.Weapons.Ranged.Overheat;

/// <summary>
/// Перегрев оружия
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnCoGunOverheatComponent : Component
{
    /// <summary>
    /// Текущий перегрев
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentHeat;

    /// <summary>
    /// Добавление тепла за выстрел
    /// </summary>
    [DataField]
    public float HeatPerShot = 0.1f;

    /// <summary>
    /// Охлождение в секунду
    /// </summary>
    [DataField]
    public float CooldownRate = 0.05f;

    /// <summary>
    /// Лимит перегрева после которого оружие блокируется
    /// </summary>
    [DataField]
    public float OverheatThreshold = 1.0f;

    /// <summary>
    /// Необходимый минимум для остывания
    /// </summary>
    [DataField]
    public float UnlockThreshold = 0f;

    /// <summary>
    /// Заблокированно оружие иза перегрева
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Overheated;

    /// <summary>
    /// Визуальный эффект покраснения
    /// </summary>
    [DataField]
    public bool VisualOverheat = true;

    /// <summary>
    /// Спам попаутами что оружие перегрето
    /// </summary>
    [DataField]
    public float PopupInterval = 2f;

    public float NextPopupTime;

    /// <summary>
    /// Урон если довести оружие до перегрева
    /// </summary>
    [DataField]
    public DamageSpecifier? TouchDamage;

    /// <summary>
    /// Звук ожога об оружие
    /// </summary>
    [DataField]
    public SoundSpecifier? TouchSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    /// <summary>
    /// Звук при перегреве оружия
    /// </summary>
    [DataField]
    public SoundSpecifier? OverheatSound;
}
