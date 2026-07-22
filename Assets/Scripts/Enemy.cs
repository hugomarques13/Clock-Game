using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An enemy you place by hand in the scene. Each turn it picks a random
/// cardinal direction and telegraphs a 3-tile attack in red: the tile straight
/// ahead plus the two diagonals either side of it.
///
/// e.g. facing right, from cell (0,0) it threatens (1,0), (1,1) and (1,-1).
///
/// The attack only lands when the player's turn ends. Kill the enemy before
/// then and the attack never happens.
/// </summary>
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    [Tooltip("Damage dealt to the player per attack.")]
    public int damage = 1;

    [Header("Telegraph colours")]
    public Color warningColor = new Color(1f, 0f, 0f, 0.35f);
    public Color strikeColor = new Color(1f, 0.15f, 0.15f, 0.85f);

    [Tooltip("Draw order for the red squares. Must be above your tiles, below the player.")]
    public int highlightSortingOrder = 8;

    public Vector3Int Cell { get; private set; }
    public Vector3Int AttackDirection { get; private set; }

    readonly List<GameObject> highlights = new List<GameObject>();

    /// <summary>Finds the enemy standing on a cell, or null.</summary>
    public static Enemy At(Vector3Int cell)
    {
        foreach (var e in All)
            if (e != null && e.Cell == cell) return e;
        return null;
    }

    void Awake()
    {
        All.Add(this);

        // Snap onto the grid here, not in Start: TurnManager.Start() telegraphs
        // the first turn and Unity does not guarantee our Start runs before it.
        Cell = GridUtils.WorldToCell(transform.position);
        transform.position = GridUtils.CellToWorld(Cell, transform.position.z);
    }

    void OnDestroy()
    {
        All.Remove(this);
        ClearHighlights();
    }

    /// <summary>The three cells this attack will hit.</summary>
    public IEnumerable<Vector3Int> AttackCells()
    {
        Vector3Int dir = AttackDirection;
        // Perpendicular: (1,0) -> (0,1), (0,1) -> (1,0). Gives the two diagonals.
        Vector3Int perp = new Vector3Int(dir.y, dir.x, 0);

        yield return Cell + dir;
        yield return Cell + dir + perp;
        yield return Cell + dir - perp;
    }

    /// <summary>Rolls a new direction and paints the warning tiles.</summary>
    public void ChooseNewAttack()
    {
        ClearHighlights();

        AttackDirection = GridUtils.Cardinals[Random.Range(0, GridUtils.Cardinals.Length)];

        foreach (var c in AttackCells())
        {
            highlights.Add(TileHighlight.Spawn(c, warningColor, highlightSortingOrder, "AttackWarning"));
        }
    }

    /// <summary>Fires the telegraphed attack. Called during resolution.</summary>
    public void ExecuteAttack()
    {
        // Flash the warning tiles brighter so the hit reads on screen.
        foreach (var h in highlights)
        {
            if (h == null) continue;
            var sr = h.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = strikeColor;
        }

        var player = PlayerMovement.Instance;
        if (player == null) return;

        foreach (var c in AttackCells())
        {
            if (c != player.Cell) continue;

            var hp = player.GetComponent<Health>();
            if (hp != null) hp.TakeDamage(damage);
            break;   // one enemy attack hits the player at most once
        }
    }

    void ClearHighlights()
    {
        foreach (var h in highlights)
            if (h != null) Destroy(h);

        highlights.Clear();
    }
}
