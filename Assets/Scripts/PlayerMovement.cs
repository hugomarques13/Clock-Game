using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Grid-based top-down movement, plus placing projectiles with Z.
///
/// The player occupies exactly one cell and slides cell-to-cell in the 4
/// cardinal directions - never diagonally. Both moving and placing are only
/// allowed while the clock is running (TurnPhase.PlayerTurn); during
/// resolution the player is frozen.
///
/// No Rigidbody2D needed: this moves the Transform directly and checks the
/// destination cell for obstacles before committing.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Movement")]
    [Tooltip("Seconds to slide from one cell to the next. Lower = faster.")]
    public float timePerCell = 0.12f;

    [Tooltip("Keep moving while the key is held. Off = one cell per key press.")]
    public bool holdToRepeat = true;

    [Header("Projectiles (Z)")]
    [Tooltip("Optional. Leave empty to use a generated yellow square.")]
    public GameObject projectilePrefab;

    [Tooltip("Direction every projectile travels. (1,0,0) is right.")]
    public Vector3Int projectileDirection = Vector3Int.right;

    [Tooltip("Max tiles a projectile travels per resolution.")]
    public int projectileRange = 5;

    public int projectileDamage = 1;

    public Vector3Int Cell { get; private set; }

    Vector3 moveStart;
    Vector3 moveEnd;
    float moveTimer;
    bool isMoving;

    void Awake()
    {
        Instance = this;

        // The player should stay in the scene when killed, not vanish.
        var hp = GetComponent<Health>();
        if (hp != null) hp.destroyOnDeath = false;

        // Snap onto the grid in Awake so anything reading Cell during Start
        // (enemy telegraphs, the HUD) sees the real value.
        Cell = GridUtils.WorldToCell(transform.position);
        transform.position = GridUtils.CellToWorld(Cell, transform.position.z);
    }

    void Update()
    {
        // Frozen while everything resolves.
        var tm = TurnManager.Instance;
        if (tm != null && tm.Phase != TurnPhase.PlayerTurn) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.zKey.wasPressedThisFrame) PlaceProjectile();

        if (isMoving)
        {
            Slide();
            return;
        }

        Vector3Int dir = ReadDirection(kb);
        if (dir != Vector3Int.zero) TryMove(dir);
    }

    /// <summary>Reads WASD/arrows and returns ONE cardinal direction, never a diagonal.</summary>
    Vector3Int ReadDirection(Keyboard kb)
    {
        // When holdToRepeat is off we only react to the frame the key goes down.
        bool up    = holdToRepeat ? (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                                  : (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame);
        bool down  = holdToRepeat ? (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                                  : (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame);
        bool left  = holdToRepeat ? (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                                  : (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame);
        bool right = holdToRepeat ? (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                                  : (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame);

        // Checked in order, so pressing two keys at once picks one instead of
        // going diagonal.
        if (up)    return Vector3Int.up;
        if (down)  return Vector3Int.down;
        if (left)  return Vector3Int.left;
        if (right) return Vector3Int.right;
        return Vector3Int.zero;
    }

    void TryMove(Vector3Int dir)
    {
        Vector3Int target = Cell + dir;

        if (GridUtils.IsWall(target)) return;
        if (Enemy.At(target) != null) return;   // can't walk onto an enemy

        Cell = target;
        moveStart = transform.position;
        moveEnd = GridUtils.CellToWorld(target, transform.position.z);
        moveTimer = 0f;
        isMoving = true;
    }

    void Slide()
    {
        moveTimer += Time.deltaTime;

        if (timePerCell <= 0f || moveTimer >= timePerCell)
        {
            transform.position = moveEnd;   // land exactly on the cell, no drift
            isMoving = false;
            return;
        }

        transform.position = Vector3.Lerp(moveStart, moveEnd, moveTimer / timePerCell);
    }

    void PlaceProjectile()
    {
        var tm = TurnManager.Instance;

        GameObject go;
        if (projectilePrefab != null)
        {
            go = Instantiate(projectilePrefab, GridUtils.CellToWorld(Cell), Quaternion.identity);
        }
        else
        {
            // No art assigned - use a small generated square so it still works.
            go = TileHighlight.Spawn(Cell, new Color(1f, 0.9f, 0.2f, 1f), 15, "Projectile");
            go.transform.localScale *= 0.4f;
        }
        go.name = "Projectile";

        var p = go.GetComponent<Projectile>();
        if (p == null) p = go.AddComponent<Projectile>();

        p.direction = projectileDirection;
        p.range = projectileRange;
        p.damage = projectileDamage;

        if (tm != null) tm.SpendTime(tm.projectileTimeCost);
    }

    // Draws a yellow box in the Scene view showing the cell we occupy.
    void OnDrawGizmosSelected()
    {
        var g = GridUtils.Grid;
        if (g == null) return;

        Gizmos.color = Color.yellow;
        Vector3Int c = g.WorldToCell(transform.position);
        Gizmos.DrawWireCube(g.GetCellCenterWorld(c), g.cellSize);
    }
}
