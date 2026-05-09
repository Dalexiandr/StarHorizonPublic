using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Ore;

/// <summary>
/// Сканер который показывает количество руды в жилах
/// </summary>
[RegisterComponent]
public sealed partial class OreScannerComponent : Component
{
    /// <summary>
    /// Уровни сканера, максимальный - 5
    /// </summary>
    [DataField("scanLevel")]
    public int ScanLevel = 1;
}
