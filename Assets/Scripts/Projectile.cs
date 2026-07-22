using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A projectile placed by the player during their turn. It sits still until
/// the clock runs out, then advances one cell at a time during resolution -
/// up to its range, or until it hits a wall or an enemy.
///
/// Each projectile carries a small random visual offset so several sharing a
/// cell don't stack into one blob. The offset is cosmetic only: Cell is still
/// the exact grid position used for movement, walls and hit detection.
/// </summary>
public class Projectile : MonoBehaviour
{
    public static readonly List<Projectile> All = new List<Projectile>();

    [Tooltip("Which way it travels. (1,0,0) is right.")]
    public Vector3Int direction = Vector3Int.right;

    [Tooltip("Maximum tiles travelled per resolution.")]
    public int range = 5;

    public int damage = 1;

    [Header("Visual scatter")]
    [Tooltip("How far it can sit from the cell centre, as a fraction of the cell. 0 = dead centre.")]
    [Range(0f, 1f)]
    public float scatter = 0.45f;

    [Tooltip("Random spots tried when placing. Higher = better spread, slightly more work.")]
    public int scatterCandidates = 8;

    public Vector3Int Cell { get; private set; }

    Vector3 visualOffset;
    int stepsTaken;
    bool placed;

    /// <summary>True while at least one projectile still has travel left.</summary>
    public static bool AnyActive
    {
        get
        {
            foreach (var p in All)
                if (p != null && p.stepsTaken < p.range) return true;
            return false;
        }
    }

    /// <summary>Advances every projectile by one cell.</summary>
    public static void StepAll()
    {
        // Backwards so a projectile removing itself can't break the loop.
        for (int i = All.Count - 1; i >= 0; i--)
        {
            if (i < All.Count && All[i] != null) All[i].Step();
        }
    }

    void Awake()
    {
        All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    void Start()
    {
        // Safety net if something spawned this without calling PlaceAt.
        if (!placed) PlaceAt(GridUtils.WorldToCell(transform.position));
    }

    /// <summary>
    /// Drops the projectile onto a cell and picks its visual offset. Call this
    /// after setting scatter, so the settings are actually applied.
    /// </summary>
    public void PlaceAt(Vector3Int cell)
    {
        Cell = cell;
        placed = true;
        visualOffset = PickOffset();
        ApplyPosition();
    }

    /// <summary>
    /// Samples a few random spots inside the cell and keeps whichever sits
    /// furthest from the projectiles already there.
    /// </summary>
    Vector3 PickOffset()
    {
        if (scatter <= 0f) return Vector3.zero;

        Vector2 cell = GridUtils.CellSize;
        float rx = cell.x * scatter * 0.5f;
        float ry = cell.y * scatter * 0.5f;

        Vector3 best = Vector3.zero;
        float bestClearance = -1f;
        int tries = Mathf.Max(1, scatterCandidates);

        for (int i = 0; i < tries; i++)
        {
            var candidate = new Vector3(Random.Range(-rx, rx), Random.Range(-ry, ry), 0f);
            float clearance = NearestNeighbourDistance(candidate);

            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                best = candidate;
            }

            // Nothing else in this cell, so any spot is as good as another.
            if (bestClearance == float.MaxValue) break;
        }

        return best;
    }

    /// <summary>Distance from a candidate spot to the closest projectile sharing our cell.</summary>
    float NearestNeighbourDistance(Vector3 candidate)
    {
        Vector3 world = GridUtils.CellToWorld(Cell) + candidate;
        float nearest = float.MaxValue;

        foreach (var p in All)
        {
            if (p == null || p == this || p.Cell != Cell) continue;
            nearest = Mathf.Min(nearest, Vector2.Distance(world, p.transform.position));
        }

        return nearest;
    }

    void ApplyPosition()
    {
        Vector3 p = GridUtils.CellToWorld(Cell, transform.position.z) + visualOffset;
        transform.position = p;
    }

    void Step()
    {
        if (stepsTaken >= range) return;

        Vector3Int next = Cell + direction;

        // Ran into a wall - stop here and disappear.
        if (GridUtils.IsWall(next))
        {
            Destroy(gameObject);
            return;
        }

        Cell = next;
        stepsTaken++;
        ApplyPosition();   // keeps its offset, so it stays visually distinct in flight

        // Hit an enemy? Damage it and the projectile is spent.
        Enemy hit = Enemy.At(Cell);
        if (hit != null)
        {
            var hp = hit.GetComponent<Health>();
            if (hp != null) hp.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // Out of range.
        if (stepsTaken >= range) Destroy(gameObject);
    }
}
