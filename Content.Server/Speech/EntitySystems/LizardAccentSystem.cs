using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class LizardAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");
    // Horizon start: Russian letter support
    private static readonly Regex RegexC = new("[сС]+", RegexOptions.None);
    private static readonly Regex RegexSh = new("[шШ]+", RegexOptions.None);
    private static readonly Regex RegexZ = new("[зЗ]+", RegexOptions.None);
    private static readonly Regex RegexShch = new("[щЩ]+", RegexOptions.None);
    // Horizon end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, LizardAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // hissss
        message = RegexLowerS.Replace(message, "sss");
        // hiSSS
        message = RegexUpperS.Replace(message, "SSS");
        // ekssit
        message = RegexInternalX.Replace(message, "$1kss");
        // ecks
        message = RegexLowerEndX.Replace(message, "ecks$1");
        // eckS
        message = RegexUpperEndX.Replace(message, "ECKS$1");
        // Horizon start: Russian letter replacements for reptilian accent
        // с -> ссс, С -> ССС
        message = RegexC.Replace(message, m => {
            var firstChar = m.Value[0];
            return char.IsUpper(firstChar) ? "ССС" : "ссс";
        });
        // ш -> шшш, Ш -> ШШШ
        message = RegexSh.Replace(message, m => {
            var firstChar = m.Value[0];
            return char.IsUpper(firstChar) ? "ШШШ" : "шшш";
        });
        // з -> ззз, З -> ЗЗЗ
        message = RegexZ.Replace(message, m => {
            var firstChar = m.Value[0];
            return char.IsUpper(firstChar) ? "ЗЗЗ" : "ззз";
        });
        // щ -> щщщ, Щ -> ЩЩЩ
        message = RegexShch.Replace(message, m => {
            var firstChar = m.Value[0];
            return char.IsUpper(firstChar) ? "ЩЩЩ" : "щщщ";
        });
        // Horizon end

        args.Message = message;
    }
}
