using UnityEngine;

/// <summary>
/// Prototype HUD: health bar top-left, clock top-right.
///
/// Deliberately drawn with OnGUI so it needs zero setup - no Canvas, no fonts,
/// no prefabs. Put it on the same GameObject as the TurnManager. Swap it for a
/// real uGUI/TextMeshPro canvas once the mechanics feel right.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Tooltip("Leave empty to find the Player automatically.")]
    public Health playerHealth;

    public int fontSize = 26;

    TurnManager turns;
    GUIStyle labelStyle;

    void Start()
    {
        turns = TurnManager.Instance;

        if (playerHealth == null && PlayerMovement.Instance != null)
            playerHealth = PlayerMovement.Instance.GetComponent<Health>();
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = fontSize;
            labelStyle.fontStyle = FontStyle.Bold;
        }

        DrawHealth();
        DrawClock();
    }

    void DrawHealth()
    {
        if (playerHealth == null) return;

        const float x = 20f, y = 20f, w = 240f, h = 26f;

        float fill = playerHealth.maxHealth > 0
            ? (float)playerHealth.Current / playerHealth.maxHealth
            : 0f;

        // Background then fill.
        DrawRect(new Rect(x, y, w, h), new Color(0f, 0f, 0f, 0.6f));
        DrawRect(new Rect(x, y, w * fill, h), Color.Lerp(Color.red, Color.green, fill));

        labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 8f, y, w, h + 4f),
                  "HP  " + playerHealth.Current + " / " + playerHealth.maxHealth, labelStyle);

        if (playerHealth.IsDead)
        {
            GUI.Label(new Rect(x, y + h + 8f, 400f, 40f), "DEAD", labelStyle);
        }
    }

    void DrawClock()
    {
        if (turns == null) turns = TurnManager.Instance;
        if (turns == null) return;

        const float w = 220f, h = 40f;
        float x = Screen.width - w - 20f;
        float y = 20f;

        bool resolving = turns.Phase != TurnPhase.PlayerTurn;

        // Turn red as the clock runs low so the pressure reads at a glance.
        labelStyle.normal.textColor = resolving ? new Color(1f, 0.6f, 0.2f)
                                    : turns.TimeLeft <= 3f ? Color.red
                                    : Color.white;

        labelStyle.alignment = TextAnchor.MiddleRight;

        string text = resolving
            ? "RESOLVING..."
            : turns.TimeLeft.ToString("0.0") + "s";

        DrawRect(new Rect(x, y, w, h), new Color(0f, 0f, 0f, 0.6f));
        GUI.Label(new Rect(x - 10f, y, w, h), text, labelStyle);

        labelStyle.alignment = TextAnchor.UpperLeft;   // reset for the health label

        // Small hint line, handy while prototyping.
        var small = new GUIStyle(GUI.skin.label);
        small.fontSize = 14;
        small.alignment = TextAnchor.UpperRight;
        small.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
        GUI.Label(new Rect(x - 10f, y + h + 4f, w, 40f),
                  "Turn " + turns.TurnNumber + "   -   Z = place (-" + turns.projectileTimeCost + "s)", small);
    }

    static void DrawRect(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
