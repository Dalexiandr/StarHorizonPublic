using Robust.Shared.GameStates;
using Content.Shared.Chat.Components;  // Добавьте эту строку

namespace Content.Shared.Chat.Components;

[RegisterComponent]
[NetworkedComponent]  // Уберите если не нужно синхронизировать
public sealed partial class OocSpamProtectionComponent : Component
{
    [DataField("maxTrackedMessages")]
    public int MaxTrackedMessages = 5;

    [DataField("spamTriggerCount")]
    public int SpamTriggerCount = 3;

    [DataField("spamTimeThreshold")]
    public float SpamTimeThreshold = 0.5f;

    [DataField("muteDuration")]
    public float MuteDuration = 30f;

    [DataField("escalationMultiplier")]
    public float EscalationMultiplier = 1.5f;

    [DataField("maxMuteDuration")]
    public float MaxMuteDuration = 300f;

    // Массивы для истории сообщений (только на сервере)
    public string[] RecentMessages = Array.Empty<string>();
    public TimeSpan[] MessageTimestamps = Array.Empty<TimeSpan>();
    public int MessageCount = 0;
}
