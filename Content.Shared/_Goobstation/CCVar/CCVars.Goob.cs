using Robust.Shared.Configuration;

namespace Content.Shared._Goobstation.CCVar;

[CVarDefs]
public sealed partial class GoobCVars
{

    /// <summary>
    ///     Goobstation: The amount of time between NPC Silicons draining their battery in seconds.
    /// </summary>
    public static readonly CVarDef<float> SiliconNpcUpdateTime =
        CVarDef.Create("silicon.npcupdatetime", 1.5f, CVar.SERVERONLY);

}
