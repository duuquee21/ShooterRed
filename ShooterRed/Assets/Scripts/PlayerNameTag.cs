using Fusion;
using UnityEngine;

// Se pone en la escena (no en el prefab del jugador).
// Itera todos los jugadores cada frame y dibuja su nombre encima con OnGUI.
public class PlayerNameTag : MonoBehaviour
{
    [Header("Estilo")]
    [SerializeField] private int fontSize = 16;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color localColor = Color.green;
    [SerializeField] private float heightOffset = 2.4f; // metros sobre el origen del jugador

    private GUIStyle _style;
    private Camera _cam;

    private void Update()
    {
        // Buscamos la cámara activa cada frame por si cambia (spawn/respawn)
        if (_cam == null || !_cam.enabled)
            _cam = Camera.main;
    }

    private void OnGUI()
    {
        if (_cam == null) return;
        if (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null) return;
        if (!NetworkManager.Instance.Runner.IsRunning) return;

        EnsureStyle();

        NetworkRunner runner = NetworkManager.Instance.Runner;

        // Iteramos todos los jugadores conectados
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            NetworkObject obj = runner.GetPlayerObject(player);
            if (obj == null) continue;

            PlayerState ps = obj.GetComponent<PlayerState>();
            if (ps == null) continue;

            // No mostrar etiqueta si está muerto
            if (ps.Health <= 0) continue;

            string name = ps.PlayerName.ToString();
            if (string.IsNullOrEmpty(name)) name = "Jugador " + player.PlayerId;

            // Posición en mundo: encima de la cabeza
            Vector3 worldPos = obj.transform.position + Vector3.up * heightOffset;

            // Convertir a coordenadas de pantalla
            Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);

            // Si está detrás de la cámara, no dibujar
            if (screenPos.z < 0) continue;

            // OnGUI usa Y invertido respecto a WorldToScreenPoint
            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // Color: verde para el jugador local, rojo para enemigos
            bool isLocal = player == runner.LocalPlayer;
            _style.normal.textColor = isLocal ? localColor : enemyColor;

            // Calcular ancho del texto para centrarlo
            GUIContent content = new GUIContent(name);
            Vector2 size = _style.CalcSize(content);

            GUI.Label(new Rect(guiX - size.x * 0.5f, guiY - size.y * 0.5f, size.x, size.y), content, _style);
        }
    }

    private void EnsureStyle()
    {
        if (_style != null) return;
        _style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }
}
