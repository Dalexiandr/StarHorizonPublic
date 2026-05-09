using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.FlavorText;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class FactionRequirment : JobRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<CharacterFactionPrototype>> Factions = new();

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (profile is null)
            return true;

        var sb = new StringBuilder();
        sb.Append("[color=yellow]");
        foreach (var s in Factions)
        {
            sb.Append(Loc.GetString(protoManager.Index(s).Name) + " ");
        }

        sb.Append("[/color]");

        if (!Inverted)
        {
            reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-whitelisted-factions")}\n{sb}");

            if (!Factions.Contains(profile.Faction))
                return false;
        }
        else
        {
            reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-blacklisted-factions")}\n{sb}");

            if (Factions.Contains(profile.Faction))
                return false;
        }

        return true;
    }
}
