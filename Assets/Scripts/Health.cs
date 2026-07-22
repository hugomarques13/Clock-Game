using System;
using UnityEngine;

/// <summary>
/// Hit points for anything that can be damaged. Goes on the Player and on
/// every Enemy.
/// </summary>
public class Health : MonoBehaviour
{
    public int maxHealth = 3;

    [Tooltip("Enemies should destroy themselves on death. The Player should not.")]
    public bool destroyOnDeath = true;

    public int Current { get; private set; }
    public bool IsDead { get { return Current <= 0; } }

    /// <summary>Fires whenever health changes, so the HUD can redraw.</summary>
    public event Action Changed;

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Current = Mathf.Max(0, Current - amount);
        if (Changed != null) Changed();

        Debug.Log(name + " took " + amount + " damage (" + Current + "/" + maxHealth + ")", this);

        if (Current <= 0) Die();
    }

    void Die()
    {
        Debug.Log(name + " died.", this);

        if (destroyOnDeath) Destroy(gameObject);
    }
}
