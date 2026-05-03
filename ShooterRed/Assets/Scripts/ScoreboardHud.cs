using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ScoreboardHud : MonoBehaviour
{
    [Header("Tecla")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Estilo")]
    [SerializeField] private int fontSize = 18;
    [SerializeField] private Color headerColor = Color.yellow;
    [SerializeField] private Color rowColor = Color.white;
    [SerializeField] private Color localPlayerColor = Color.cyan;

    private struct ScoreboardEntry
    {
        public int PlayerId;
        public string Name;
        public int Kills;
        public int Deaths;
        public int Score;
    }

    private GUIStyle _headerStyle;
    private GUIStyle _rowStyle;
    private GUIStyle _localStyle;

    private void OnGUI()
    {
        // 1. Primero comprobamos que la red y el GameState existen
        if (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null)
            return;

        if (!NetworkManager.Instance.Runner.IsRunning)
            return;

        if (!GameState.TryGetInstance(out GameState gs) || gs == null)
            return;

        // 2. Comprobamos si la partida ha terminado
        bool matchEnded = gs.State == MatchState.Ended;

        // 3. LA MAGIA: Si NO estamos pulsando TAB y la partida NO ha terminado, salimos.
        // Si la partida ha terminado, esta línea no se cumple y sigue dibujando.
        if (!Input.GetKey(toggleKey) && !matchEnded)
            return;

        EnsureStyles();

        List<ScoreboardEntry> entries = BuildEntries(gs);
        entries.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Le pasamos la variable matchEnded para dibujar un título especial si se acabó
        DrawScoreboard(entries, matchEnded); 
    }

    private List<ScoreboardEntry> BuildEntries(GameState gs)
    {
        List<ScoreboardEntry> entries = new List<ScoreboardEntry>();

        foreach (var kvp in gs.Players)
        {
            entries.Add(new ScoreboardEntry
            {
                PlayerId = kvp.Key.PlayerId,
                Name = gs.GetPlayerName(kvp.Key),
                Kills = kvp.Value.Kills,
                Deaths = kvp.Value.Deaths,
                Score = kvp.Value.Score
            });
        }

        return entries;
    }

    private void DrawScoreboard(List<ScoreboardEntry> entries, bool matchEnded)
    {
        int localId = NetworkManager.Instance.Runner.LocalPlayer.PlayerId;

        float rowH = fontSize + 10;
        float panelW = 500f;
        
        // Si terminó la partida, hacemos el panel un poco más alto para que quepa el título
        float titleOffset = matchEnded ? 40f : 0f; 
        float panelH = rowH * (entries.Count + 2) + 30f + titleOffset;
        
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        // Fondo semitransparente (lo oscurecemos un poquito más para que resalte)
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(px - 12, py - 12, panelW + 24, panelH + 24), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float y = py;

        // ==========================================
        // NUEVO: Título final si la partida terminó
        // ==========================================
        if (matchEnded)
        {
            GUIStyle titleStyle = new GUIStyle(_headerStyle);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = fontSize + 8; // Letra más grande
            GUI.Label(new Rect(px, y, panelW, 30f), "¡RESULTADOS FINALES!", titleStyle);
            y += titleOffset; // Bajamos la tabla para no pisar el título
        }

        float col0 = px;
        float col1 = px + 220;
        float col2 = px + 320;
        float col3 = px + 420;

        // Cabecera
        GUI.Label(new Rect(col0, y, 210, rowH), "JUGADOR", _headerStyle);
        GUI.Label(new Rect(col1, y, 90, rowH), "KILLS", _headerStyle);
        GUI.Label(new Rect(col2, y, 90, rowH), "DEATHS", _headerStyle);
        GUI.Label(new Rect(col3, y, 80, rowH), "SCORE", _headerStyle);
        y += rowH * 1.4f;

        foreach (ScoreboardEntry e in entries)
        {
            GUIStyle style = e.PlayerId == localId ? _localStyle : _rowStyle;
            string name = e.Name + (e.PlayerId == localId ? " (tu)" : "");

            GUI.Label(new Rect(col0, y, 210, rowH), name, style);
            GUI.Label(new Rect(col1, y, 90, rowH), e.Kills.ToString(), style);
            GUI.Label(new Rect(col2, y, 90, rowH), e.Deaths.ToString(), style);
            GUI.Label(new Rect(col3, y, 80, rowH), e.Score.ToString(), style);
            y += rowH;
        }
    }

    private void EnsureStyles()
    {
        if (_headerStyle != null)
            return;

        _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };
        _headerStyle.normal.textColor = headerColor;

        _rowStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
        _rowStyle.normal.textColor = rowColor;

        _localStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };
        _localStyle.normal.textColor = localPlayerColor;
    }
}