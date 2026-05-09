using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using Vector2 = System.Numerics.Vector2;

namespace Content.Client.Shuttles.UI;

public partial class BaseShuttleControl
{
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    // Per-draw caching
    private readonly List<(Vector2i, ContentTileDefinition)> _gridTileList = new();
    // Stores inward directions of borders.
    private readonly Dictionary<Vector2i, DirectionFlag> _gridNeighborSet = new();
    private readonly List<(Vector2 Start, Vector2 End)> _edges = new();

    private void RebuildGridData(Entity<MapGridComponent> grid, GridDrawData gridData)
    {
        // Okay so there's 2 steps to this
        // 1. Build a set of triangle-strip data for each tile.
        // 2. Build edge data (line-strip decomposition) for neighbor-aware border rendering.
        _gridTileList.Clear();
        _gridNeighborSet.Clear();
        _edges.Clear();

        var rator = Maps.GetAllTilesEnumerator(grid.Owner, grid.Comp);
        var tileSize = grid.Comp.TileSize;

        while (rator.MoveNext(out var tileRef))
        {
            var index = tileRef.Value.GridIndices;

            // Drawing logic rewritten: use a convex polygon defined on the tile prototype.
            var def = (ContentTileDefinition) _tileDef[tileRef.Value.Tile.TypeId];
            _gridTileList.Add((index, def));

            // Since our shape has to be convex, draw it by taking our first vertex as origin.
            var bl = Maps.TileToVector(grid, index);
            var origin = bl + def.Vertices[0] * tileSize;
            var prev = bl + def.Vertices[1] * tileSize;

            for (var i = 2; i < def.Vertices.Count; i++)
            {
                var vert = bl + def.Vertices[i] * tileSize;
                gridData.Vertices.Add(origin);
                gridData.Vertices.Add(prev);
                gridData.Vertices.Add(vert);
                prev = vert;
            }

            // Also check our neighbours for edge visibility.
            // Note: we store inward directions, so they're inverted here.
            var dirFlag = DirectionFlag.None;
            prev = def.Vertices[def.Vertices.Count - 1];

            for (var i = 0; i < def.Vertices.Count; i++)
            {
                var vert = def.Vertices[i];

                // Check if this line is adjacent to any cardinal direction edge.
                if (prev.X == 0 && vert.X == 0)
                    dirFlag |= DirectionFlag.East;
                else if (prev.X == 1 && vert.X == 1)
                    dirFlag |= DirectionFlag.West;
                else if (prev.Y == 0 && vert.Y == 0)
                    dirFlag |= DirectionFlag.North;
                else if (prev.Y == 1 && vert.Y == 1)
                    dirFlag |= DirectionFlag.South;

                prev = vert;
            }

            _gridNeighborSet[index] = dirFlag;
        }

        gridData.EdgeIndex = gridData.Vertices.Count;

        foreach (var (index, def) in _gridTileList)
        {
            var bl = Maps.TileToVector(grid, index);

            // Start from drawing the end->start line.
            var prev = def.Vertices[def.Vertices.Count - 1];

            for (var i = 0; i < def.Vertices.Count; i++)
            {
                var vert = def.Vertices[i];

                // If this line is adjacent to a cardinal direction edge and we have a neighbour there,
                // then that's not an edge.
                var dirFlag = DirectionFlag.None;
                if (prev.X == 0 && vert.X == 0)
                    dirFlag = DirectionFlag.West;
                else if (prev.X == 1 && vert.X == 1)
                    dirFlag = DirectionFlag.East;
                else if (prev.Y == 0 && vert.Y == 0)
                    dirFlag = DirectionFlag.South;
                else if (prev.Y == 1 && vert.Y == 1)
                    dirFlag = DirectionFlag.North;

                if (dirFlag != DirectionFlag.None
                    && _gridNeighborSet.TryGetValue(index + dirFlag.AsDir().ToIntVec(), out var otherNeighbours)
                    && (otherNeighbours & dirFlag) != 0)
                {
                    prev = vert;
                    continue;
                }

                _edges.Add((bl + prev * tileSize, bl + vert * tileSize));
                prev = vert;
            }
        }

        // Decompose the edges into longer lines to save data.
        // Now we decompose the lines into longer lines (less data to send to the GPU).
        var decomposed = true;
        while (decomposed)
        {
            decomposed = false;

            for (var i = 0; i < _edges.Count; i++)
            {
                var (start, end) = _edges[i];
                var neighborFound = false;
                var neighborIndex = 0;
                Vector2 neighborStart;
                var neighborEnd = Vector2.Zero;

                // Does our end correspond with another start?
                for (var j = i + 1; j < _edges.Count; j++)
                {
                    (neighborStart, neighborEnd) = _edges[j];
                    if (!end.Equals(neighborStart))
                        continue;

                    neighborFound = true;
                    neighborIndex = j;
                    break;
                }

                if (!neighborFound)
                    continue;

                // Check if our start and the neighbor's end are collinear.
                if (!CollinearSimplifier.IsCollinear(start, end, neighborEnd, 10f * float.Epsilon))
                    continue;

                decomposed = true;
                _edges[i] = (start, neighborEnd);
                _edges.RemoveAt(neighborIndex);
            }
        }

        gridData.Vertices.EnsureCapacity(_edges.Count * 2);

        foreach (var edge in _edges)
        {
            gridData.Vertices.Add(edge.Start);
            gridData.Vertices.Add(edge.End);
        }

        gridData.LastBuild = grid.Comp.LastTileModifiedTick;
    }
}

