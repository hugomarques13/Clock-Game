using System.Collections;
using UnityEngine;

public enum TurnPhase
{
    PlayerTurn,   // clock ticking, player free to move and place
    Resolving,    // clock empty, player frozen, everything plays out
}

/// <summary>
/// Runs the clock and the turn loop. Put this on one empty GameObject named
/// "GameManager". There should only ever be one in the scene.
///
/// The loop:
///   1. PlayerTurn  - 10s counts down in real time. Moving is free,
///                    pressing Z to place a projectile costs 2s.
///   2. Resolving   - projectiles fly first, then surviving enemies attack.
///   3. Enemies roll a new direction, clock resets to 10s, back to step 1.
///
/// Projectiles resolving before enemy attacks is deliberate: killing an enemy
/// during resolution cancels the attack it had telegraphed.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Clock")]
    [Tooltip("Seconds the player gets each turn.")]
    public float turnSeconds = 10f;

    [Tooltip("Seconds removed from the clock for placing a projectile with Z.")]
    public float projectileTimeCost = 2f;

    [Header("Resolution pacing")]
    [Tooltip("Seconds between each tile a projectile advances. Purely visual.")]
    public float projectileStepDelay = 0.07f;

    [Tooltip("How long the attack flash stays on screen before the next turn.")]
    public float attackFlashSeconds = 0.35f;

    [Header("World")]
    [Tooltip("Layers that block movement and stop projectiles. Set your Walls tilemap's layer here.")]
    public LayerMask obstacleLayers;

    public float TimeLeft { get; private set; }
    public TurnPhase Phase { get; private set; }
    public int TurnNumber { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one TurnManager in the scene. Disabling the extra one.", this);
            enabled = false;
            return;
        }
        Instance = this;

        TimeLeft = turnSeconds;
        Phase = TurnPhase.PlayerTurn;
    }

    void Start()
    {
        // Every Enemy has registered itself by now, so telegraph turn one.
        TurnNumber = 1;
        foreach (var e in Enemy.All) e.ChooseNewAttack();
    }

    void Update()
    {
        if (Phase != TurnPhase.PlayerTurn) return;

        TimeLeft -= Time.deltaTime;

        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            StartCoroutine(Resolve());
        }
    }

    /// <summary>Called when the player spends clock time (placing a projectile).</summary>
    public void SpendTime(float seconds)
    {
        if (Phase != TurnPhase.PlayerTurn) return;
        TimeLeft = Mathf.Max(0f, TimeLeft - seconds);
        // Update() picks it up next frame and starts resolution if it hit zero.
    }

    IEnumerator Resolve()
    {
        Phase = TurnPhase.Resolving;

        // --- Phase 1: projectiles fly, one tile at a time ---------------
        // Keep stepping while at least one projectile still has range left.
        while (Projectile.AnyActive)
        {
            Projectile.StepAll();
            if (projectileStepDelay > 0f) yield return new WaitForSeconds(projectileStepDelay);
            else yield return null;
        }

        // --- Phase 2: enemies that survived carry out their attack ------
        // ToArray because an attack could destroy something mid-loop.
        foreach (var e in Enemy.All.ToArray())
        {
            if (e != null) e.ExecuteAttack();
        }

        yield return new WaitForSeconds(attackFlashSeconds);

        // --- Phase 3: set up the next turn ------------------------------
        foreach (var e in Enemy.All.ToArray())
        {
            if (e != null) e.ChooseNewAttack();
        }

        TurnNumber++;
        TimeLeft = turnSeconds;
        Phase = TurnPhase.PlayerTurn;
    }
}
