using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A projectile placed by the player during their turn. It sits still until
/// the clock runs out, then advances one cell at a time during resolution -
/// up to its range, or until it hits a wall or an enemy.
/// </summary>
public class Projectile : MonoBehaviour
{
    public static readonly List<Projectile> All = new List<Projectile>();

    [Tooltip("Which way it travels. (1,0,0) is right.")]
    public Vector3Int direction = Vector3Int.right;

    [Tooltip("Maximum tiles travelled per resolution.")]
    public int range = 5;

    public int damage = 1;

    public Vector3Int Cell { get; private set; }

    int stepsTaken;

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
        Cell = GridUtils.WorldToCell(transform.position);
        transform.position = GridUtils.CellToWorld(Cell, transform.position.z);
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
        transform.position = GridUtils.CellToWorld(Cell, transform.position.z);

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
