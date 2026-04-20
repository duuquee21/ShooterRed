using Fusion;
using UnityEngine;

public class CrosshairHud : MonoBehaviour
{
    [Header("Mira")]
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int size = 10;
    [SerializeField] private int thickness = 2;
    [SerializeField] private int gap = 4;

    private Texture2D _tex;

    private void Awake()
    {
        _tex = new Texture2D(1, 1);
        _tex.SetPixel(0, 0, Color.white);
        _tex.Apply();
    }

    private void OnGUI()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null)
            return;

        if (!NetworkManager.Instance.Runner.IsRunning)
            return;

        // Ocultar mira si el jugador esta muerto
        if (GameState.TryGetInstance(out GameState gs))
        {
            PlayerRef me = NetworkManager.Instance.Runner.LocalPlayer;
            if (gs.TryGetPlayerData(me, out PlayerCombatData data) && data.Health <= 0)
                return;
        }

        GUI.color = color;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        // Linea horizontal izquierda
        GUI.DrawTexture(new Rect(cx - size - gap, cy - thickness * 0.5f, size, thickness), _tex);
        // Linea horizontal derecha
        GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, size, thickness), _tex);
        // Linea vertical arriba
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - size - gap, thickness, size), _tex);
        // Linea vertical abajo
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + gap, thickness, size), _tex);
    }
}
