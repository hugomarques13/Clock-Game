using UnityEngine;

/// <summary>
/// Spawns coloured squares on grid cells (used for the red enemy attack
/// warnings). Generates its own sprite at runtime, so there is no prefab or
/// art to set up.
/// </summary>
public static class TileHighlight
{
    static Sprite square;

    static Sprite Square
    {
        get
        {
            if (square == null)
            {
                // A 1x1 white pixel at 1 pixel-per-unit = a 1x1 world-unit sprite.
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.filterMode = FilterMode.Point;
                tex.Apply();

                square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                square.name = "GeneratedSquare";
            }
            return square;
        }
    }

    /// <summary>Creates a translucent square sitting on the given cell.</summary>
    public static GameObject Spawn(Vector3Int cell, Color color, int sortingOrder, string name = "Highlight")
    {
        var go = new GameObject(name);
        go.transform.position = GridUtils.CellToWorld(cell);

        Vector2 size = GridUtils.CellSize;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return go;
    }
}
