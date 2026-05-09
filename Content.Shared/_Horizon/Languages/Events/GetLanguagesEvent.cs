namespace Content.Shared._Horizon.Language;

[ByRefEvent]
public record struct GetLanguagesEvent(EntityUid Uid)
{
    public string Current = "";
    public Dictionary<string, LanguageKnowledge> Languages = new();
    public Dictionary<string, LanguageKnowledge> Translator = new();
}
