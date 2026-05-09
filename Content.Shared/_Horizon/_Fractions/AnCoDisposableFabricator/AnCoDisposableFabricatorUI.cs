using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon._Fractions.AnCoDisposableFabricator;

[Serializable, NetSerializable]
public sealed class AnCoDisposableFabricatorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<int, AnCoDisposableFabricatorSetInfo> Sets;
    public int MaxSelectedSets;

    public AnCoDisposableFabricatorBoundUserInterfaceState(Dictionary<int, AnCoDisposableFabricatorSetInfo> sets, int max)
    {
        Sets = sets;
        MaxSelectedSets = max;
    }
}

[Serializable, NetSerializable]
public sealed class AnCoDisposableFabricatorChangeSetMessage : BoundUserInterfaceMessage
{
    public readonly int SetNumber;

    public AnCoDisposableFabricatorChangeSetMessage(int setNumber)
    {
        SetNumber = setNumber;
    }
}

[Serializable, NetSerializable]
public sealed class AnCoDisposableFabricatorApproveMessage : BoundUserInterfaceMessage
{
    public AnCoDisposableFabricatorApproveMessage() { }
}

[Serializable, NetSerializable]
public enum AnCoDisposableFabricatorUIKey : byte
{
    Key
}

[Serializable, NetSerializable, DataDefinition]
public partial struct AnCoDisposableFabricatorSetInfo
{
    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public AnCoDisposableFabricatorSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}

[Serializable, NetSerializable]
public enum AnCoDisposableFabricatorVisuals : byte
{
    IsWorking
}

[Serializable, NetSerializable]
public enum AnCoDisposableFabricatorVisualLayers : byte
{
    IsWorking
}
