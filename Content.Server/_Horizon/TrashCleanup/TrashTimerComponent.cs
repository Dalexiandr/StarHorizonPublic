using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server._Horizon.TrashCleanup;

/// <summary>
/// Добавляется к мусорным сущностям для отслеживания времени их автоматического удаления.
/// </summary>
[RegisterComponent]
public sealed partial class TrashTimerComponent : Component
{
    /// <summary>
    /// Время, когда эта сущность должна быть удалена.
    /// </summary>
    [DataField]
    public TimeSpan DespawnTime;
}
