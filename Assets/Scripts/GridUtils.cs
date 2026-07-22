using UnityEngine;

/// <summary>
/// Shared helpers for converting between world positions and grid cells.
/// Everything in the game talks to the grid through here so there is exactly
/// one definition of "where is cell (3, 2)".
/// </summary>
public static class GridUtils
{
    static Grid cached;

    public static Grid Grid
    {
        get
        {
            if (cached == null) cached = Object.FindFirstObjectByType<Grid>();
            return cached;
        }
    }

    public static Vector3Int WorldToCell(Vector3 world)
    {
        return Grid != null ? Grid.WorldToCell(world) : Vector3Int.RoundToInt(world);
    }

    /// <summary>Centre of a cell in world space, keeping the given z.</summary>
    public static Vector3 CellToWorld(Vector3Int cell, float z = 0f)
    {
        Vector3 p = Grid != null ? Grid.GetCellCenterWorld(cell) : cell;
        p.z = z;
        return p;
    }

    public static Vector2 CellSize
    {
        get { return Grid != null ? (Vector2)Grid.cellSize : Vector2.one; }
    }

    /// <summary>
    /// True if a wall occupies this cell. Walls are whatever is on the
    /// Obstacle Layers mask set on the TurnManager.
    /// </summary>
    public static bool IsWall(Vector3Int cell)
    {
        var tm = TurnManager.Instance;
        if (tm == null || tm.obstacleLayers.value == 0) return false;

        // Slightly under a full cell so we never clip the neighbouring tile.
        return Physics2D.OverlapBox(CellToWorld(cell), CellSize * 0.8f, 0f, tm.obstacleLayers) != null;
    }

    /// <summary>The four cardinal directions, in a fixed order.</summary>
    public static readonly Vector3Int[] Cardinals =
    {
        Vector3Int.right,
        Vector3Int.up,
        Vector3Int.left,
        Vector3Int.down,
    };
}
