namespace Content.Server._Horizon.AnCoEntityAutoTarget.Components;

/// <summary>
/// Компонент который добавляется пулям для автоматической наводки на цель.
/// Если сущность попадает в радиус действия пули, пуля меняет траекторию.
/// </summary>
[RegisterComponent]
public sealed partial class AnCoAutoGuidedBulletComponent : Component
{
    [DataField]
    public float Range;

    [DataField]
    public float TurnSpeed;

    [DataField]
    public EntityUid Shooter;

    [DataField]
    public EntityUid? Target;
}
