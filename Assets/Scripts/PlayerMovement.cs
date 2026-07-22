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

    [Tooltip("Camera used to read the mouse. Leave empty to use Camera.main.")]
    public Camera aimCamera;

    [Tooltip("Used only if there's no mouse or camera to aim with.")]
    public Vector3Int fallbackDirection = Vector3Int.right;

    [Tooltip("Max tiles a projectile travels per resolution.")]
    public int projectileRange = 5;

    public int projectileDamage = 1;

    [Tooltip("How far projectiles scatter inside their tile so they don't overlap. 0 = dead centre.")]
    [Range(0f, 1f)]
    public float projectileScatter = 0.45f;

    [Header("Aim preview")]
    [Tooltip("Shows which tile you're aiming at. Mouse aim is hard to read without it.")]
    public bool showAimPreview = true;

    public Color aimColor = new Color(1f, 1f, 1f, 0.3f);

    public Vector3Int Cell { get; private set; }

    Vector3 moveStart;
    Vector3 moveEnd;
    float moveTimer;
    bool isMoving;
    GameObject aimMarker;

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
        bool myTurn = tm == null || tm.Phase == TurnPhase.PlayerTurn;

        UpdateAimPreview(myTurn);

        if (!myTurn) return;

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

    /// <summary>
    /// The cardinal direction pointing most closely at the mouse.
    /// Whichever axis the mouse is further along wins, so the diagonal
    /// in-between is resolved rather than allowed.
    /// </summary>
    public Vector3Int AimDirection
    {
        get
        {
            var mouse = Mouse.current;
            var cam = aimCamera != null ? aimCamera : Camera.main;
            if (mouse == null || cam == null) return fallbackDirection;

            Vector3 screen = mouse.position.ReadValue();
            // For a 2D camera, z is the distance from the camera to our plane.
            screen.z = Mathf.Abs(cam.transform.position.z - transform.position.z);

            Vector3 world = cam.ScreenToWorldPoint(screen);

            // Measured from the cell centre, not transform.position, so aiming
            // doesn't wobble while we're mid-slide between cells.
            Vector2 delta = (Vector2)(world - GridUtils.CellToWorld(Cell, transform.position.z));

            if (delta.sqrMagnitude < 0.0001f) return fallbackDirection;

            return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? (delta.x >= 0f ? Vector3Int.right : Vector3Int.left)
                : (delta.y >= 0f ? Vector3Int.up : Vector3Int.down);
        }
    }

    /// <summary>Marks the tile directly in the aim direction. Hidden during resolution.</summary>
    void UpdateAimPreview(bool visible)
    {
        if (!showAimPreview)
        {
            if (aimMarker != null) Destroy(aimMarker);
            return;
        }

        if (aimMarker == null)
        {
            aimMarker = TileHighlight.Spawn(Cell, aimColor, 12, "AimPreview");
            aimMarker.transform.localScale *= 0.9f;
        }

        aimMarker.SetActive(visible);
        if (!visible) return;

        aimMarker.transform.position = GridUtils.CellToWorld(Cell + AimDirection);
    }

    void OnDestroy()
    {
        if (aimMarker != null) Destroy(aimMarker);
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

        p.direction = AimDirection;
        p.range = projectileRange;
        p.damage = projectileDamage;
        p.scatter = projectileScatter;

        // Explicit placement, after the settings above are set, so the scatter
        // value is the one we just assigned rather than the default.
        p.PlaceAt(Cell);

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
