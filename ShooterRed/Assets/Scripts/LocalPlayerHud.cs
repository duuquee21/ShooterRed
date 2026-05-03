using Fusion;
using UnityEngine;

public class LocalPlayerHud : MonoBehaviour
{
    [Header("Estilo")]
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int marginLeft = 20;
    [SerializeField] private int marginBottom = 20;

    private GUIStyle _style;
    private GUIStyle _topCenterStyle; // Nuevo estilo para el reloj y líder

    private PlayerCombatData _data;
    private bool _hasData;
    
    // Nuevas variables para el estado global
    private string _timerString = "";
    private string _leaderString = "";

    private void Update()
    {
        _hasData = false;
        _timerString = "";
        _leaderString = "";

        if (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null)
            return;

        if (!NetworkManager.Instance.Runner.IsRunning)
            return;

        if (!GameState.TryGetInstance(out GameState gameState) || gameState == null || !gameState.IsNetworkReady)
            return;

        // ==========================================
        // 1. OBTENER DATOS DEL JUGADOR LOCAL (Tus Stats)
        // ==========================================
        PlayerRef me = NetworkManager.Instance.Runner.LocalPlayer;

        if (gameState.TryGetPlayerData(me, out PlayerCombatData data))
        {
            _data = data;
            _hasData = true;
        }

        // ==========================================
        // 2. OBTENER ESTADO DEL TIEMPO
        // ==========================================
        if (gameState.State == MatchState.Waiting)
        {
            _timerString = "ESPERANDO JUGADORES...";
        }
        else if (gameState.State == MatchState.Ended)
        {
            _timerString = "¡FIN DE LA PARTIDA!";
        }
        else if (gameState.State == MatchState.Playing)
        {
            float? remainingTime = gameState.MatchTimer.RemainingTime(NetworkManager.Instance.Runner);
            if (remainingTime.HasValue)
            {
                int minutes = Mathf.FloorToInt(remainingTime.Value / 60);
                int seconds = Mathf.FloorToInt(remainingTime.Value % 60);
                _timerString = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            else
            {
                _timerString = "00:00";
            }
        }

        // ==========================================
        // 3. OBTENER EL LÍDER DE KILLS
        // ==========================================
        int maxKills = -1;
        PlayerRef currentLeader = PlayerRef.None;

        foreach (var kvp in gameState.Players)
        {
            if (kvp.Value.Kills > maxKills)
            {
                maxKills = kvp.Value.Kills;
                currentLeader = kvp.Key;
            }
        }

        if (currentLeader != PlayerRef.None && maxKills > 0)
        {
            string leaderName = gameState.GetPlayerName(currentLeader);
            _leaderString = $"LÍDER: {leaderName} ({maxKills} KILLS)";
        }
        else
        {
            _leaderString = "LÍDER: NINGUNO";
        }
    }

    private void OnGUI()
    {
        // === DIBUJAR RELOJ Y LÍDER (Arriba al centro) ===
        // Esto se dibuja siempre, incluso si tú estás muerto
        if (_topCenterStyle == null)
        {
            _topCenterStyle = new GUIStyle(GUI.skin.label);
            _topCenterStyle.fontStyle = FontStyle.Bold;
            _topCenterStyle.alignment = TextAnchor.UpperCenter; // Centrado
        }

        // Reloj (Más grande y amarillo)
        _topCenterStyle.fontSize = fontSize + 10;
        _topCenterStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(0, 20, Screen.width, _topCenterStyle.fontSize + 10), _timerString, _topCenterStyle);

        // Líder (Debajo del reloj, blanco)
        _topCenterStyle.fontSize = fontSize;
        _topCenterStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 20 + _topCenterStyle.fontSize + 15, Screen.width, _topCenterStyle.fontSize + 10), _leaderString, _topCenterStyle);


        // === DIBUJAR TUS ESTADÍSTICAS GLOBALES (Abajo a la izquierda) ===
        if (!_hasData)
            return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = fontSize;
            _style.fontStyle = FontStyle.Bold;
        }

        _style.normal.textColor = textColor;

        int lineHeight = fontSize + 6;
        int x = marginLeft;
        int y = Screen.height - marginBottom - lineHeight * 4;

        GUI.Label(new Rect(x, y, 300, lineHeight), "HP:    " + _data.Health, _style);
        GUI.Label(new Rect(x, y + lineHeight, 300, lineHeight), "Score: " + _data.Score, _style);
        GUI.Label(new Rect(x, y + lineHeight * 2, 300, lineHeight), "Racha: " + _data.Streak, _style);

        // Recompensas de racha disponibles
        GUIStyle rewardStyle = new GUIStyle(GUI.skin.label);
        rewardStyle.fontSize  = fontSize - 2;
        rewardStyle.fontStyle = FontStyle.Bold;

        int ry = y - lineHeight * 2;

        if (_data.HasGrenade)
        {
            rewardStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x, ry, 300, lineHeight), "[G] Granada disponible", rewardStyle);
            ry -= lineHeight;
        }
        if (_data.HasAirstrike)
        {
            rewardStyle.normal.textColor = new Color(1f, 0.5f, 0f);
            GUI.Label(new Rect(x, ry, 300, lineHeight), "[F] Ataque aereo disponible", rewardStyle);
            ry -= lineHeight;
        }
        if (_data.HasTurret)
        {
            rewardStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(x, ry, 300, lineHeight), "[T] Torreta disponible", rewardStyle);
        }
    }
}