using Robust.Shared.GameStates;

namespace Content.Shared.Chat.Components;

[RegisterComponent]
// УБРАТЬ [NetworkedComponent] если не нужно синхронизировать
[NetworkedComponent]  // УБЕРИТЕ ЭТО ЕСЛИ КОМПОНЕНТ НЕ ДОЛЖЕН СИНХРОНИЗИРОВАТЬСЯ ПО СЕТИ
// УБРАТЬ Access(typeof(SpamProtectionSystem)) - это Server-специфичная система
// [Access(typeof(SpamProtectionSystem))]  // УДАЛИТЬ ЭТУ СТРОКУ
public sealed partial class SpamProtectionComponent : Component
{
    // УБЕРИТЕ ViewVariables для массивов, если они не нужны на клиенте
    [ViewVariables]
    public string[] RecentMessages = Array.Empty<string>();

    [ViewVariables]
    public TimeSpan[] MessageTimestamps = Array.Empty<TimeSpan>();

    [ViewVariables]
    public int MessageCount = 0;

    [DataField("maxTrackedMessages")]
    public int MaxTrackedMessages = 5;

    [DataField("spamTriggerCount")]
    public int SpamTriggerCount = 3;

    [DataField("spamTimeThreshold")]
    public float SpamTimeThreshold = 0.5f;

    [DataField("muteDuration")]
    public float MuteDuration = 30f;

    [DataField("excludedPrefixes")]
    public string[] ExcludedPrefixes = { ".", "!", "//", "*", "?", "w\"", "а" };

    [DataField("excludedWords")]
    public string[] ExcludedWords = { "help", "sos", "medic", "помогите", "врач", "мед" };

    [DataField("announceToChat")]
    public bool AnnounceToChat = false;

    [DataField("allowEmotesWhileMuted")]
    public bool AllowEmotesWhileMuted = true;

    [DataField("escalationMultiplier")]
    public float EscalationMultiplier = 1.5f;

    [DataField("maxMuteDuration")]
    public float MaxMuteDuration = 300f;

    [DataField("escalationResetTime")]
    public float EscalationResetTime = 5f;
}
