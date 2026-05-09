using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Ore;

/// <summary>
/// Это используется для бура что бы он мог добывать руду с жил
/// </summary>
[RegisterComponent]
public sealed partial class OreExtractorComponent : Component
{
    /// <summary>
    /// Эфективность бура, чем больше тем быстрее
    /// </summary>
    [DataField("efficiency")]
    public float Efficiency = 1.0f;

    /// <summary>
    /// Удача бура, чем меньше тем меньше шанс выпадения руды 1 = 100% шанс
    /// </summary>
    [DataField("luck")]
    public float Luck = 1.0f;
    
    /// <summary>
    /// Звук бура который издёт при добычи, заделка под УВУ бур
    /// </summary>
    [DataField("extractingSound")]
    public string? ExtractingSound = "/Audio/_Horizon/Effects/electric-mining-drill.ogg";
    
    public float CurrentTimer = 0f;
    
    public EntityUid? PlayingSoundEntity = null;
}
