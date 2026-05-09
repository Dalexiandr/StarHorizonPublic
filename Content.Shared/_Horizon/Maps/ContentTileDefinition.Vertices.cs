using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Maps;

public sealed partial class ContentTileDefinition
{
    /// <summary>
    /// Vertices for drawing purposes. Has to be a convex shape.
    /// </summary>
    [DataField]
    public List<Vector2> Vertices = new()
        { Vector2.Zero, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
}

