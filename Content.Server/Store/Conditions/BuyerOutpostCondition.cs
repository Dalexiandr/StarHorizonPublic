using System.ComponentModel.DataAnnotations;
using Content.Shared._Horizon.FlavorText;
using Content.Shared._Horizon.OutpostCapture.Components;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Позволяет назначить какие из захваченных аванпостов будут
/// расширять список приобретений в маркетах
/// </summary>
public sealed partial class BuyerOutpostCondition : ListingCondition
{
    // Фракция которая должна захватить аванпост
    [DataField(required: true)]
    public string Faction = string.Empty;

    // Количество захваченных аванпостов
    [DataField("totalOutpostWhitelist")]
    public int OutpostCapturedCount;

    // Белый список "именованных" аванпостов
    [DataField("namedOutpostWhitelist")]
    public HashSet<string> OutpostCapturedWhitelist = [];


    public override bool Condition(ListingConditionArgs args)
    {
        if (Faction == string.Empty)
            return true;

        var outpostCapturedCount = 0;
        var namedOutpostCaptured = 0;
        var query = args.EntityManager.AllEntityQueryEnumerator<OutpostCaptureComponent>();
        while (query.MoveNext(out var outpost))
        {
            if (outpost.CapturedFaction == null || outpost.CapturedFaction != Faction)
                continue;

            if (outpost.OutpostName == string.Empty && OutpostCapturedWhitelist.Contains(outpost.OutpostName))
                namedOutpostCaptured++;

            outpostCapturedCount++;
        }

        return outpostCapturedCount >= OutpostCapturedCount && namedOutpostCaptured >= OutpostCapturedWhitelist.Count;
    }
}
