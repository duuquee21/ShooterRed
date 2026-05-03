using UnityEngine;
using TMPro; // Usamos TextMeshPro porque se ve mucho mejor
using Fusion;

public class MatchHUD : MonoBehaviour
{
    [Header("Referencias de UI")]
    public TextMeshProUGUI timerText;       // El texto para "05:00"
    public TextMeshProUGUI topPlayerText;   // El texto para "LÍDER: DUQUE (5 KILLS)"

    void Update()
    {
        // 1. Si no hay red o no ha cargado el GameState, mostramos un aviso
        if (GameState.Instance == null || GameState.Instance.Runner == null)
        {
            if (timerText) timerText.text = "CARGANDO...";
            if (topPlayerText) topPlayerText.text = "";
            return;
        }

        // 2. Si estamos esperando a que entre gente
        if (GameState.Instance.State == MatchState.Waiting)
        {
            if (timerText) timerText.text = "ESPERANDO...";
            if (topPlayerText) topPlayerText.text = "Faltan jugadores";
            return;
        }

        // 3. Si la partida ha terminado, mostramos el ganador final y "FIN"
        if (GameState.Instance.State == MatchState.Ended)
        {
            if (timerText) timerText.text = "¡FIN!";
            UpdateTopPlayer();
            return;
        }

        // 4. Si estamos en plena partida, actualizamos todo constantemente
        UpdateTimer();
        UpdateTopPlayer();
    }

    private void UpdateTimer()
    {
        if (timerText == null) return;

        // Leemos el tiempo restante del reloj de red
        float? remainingTime = GameState.Instance.MatchTimer.RemainingTime(GameState.Instance.Runner);
        
        if (remainingTime.HasValue)
        {
            // Convertimos los segundos a minutos y segundos
            int minutes = Mathf.FloorToInt(remainingTime.Value / 60);
            int seconds = Mathf.FloorToInt(remainingTime.Value % 60);
            
            // Formateamos para que siempre salgan dos dígitos (ej: 04:09)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            timerText.text = "00:00";
        }
    }

    private void UpdateTopPlayer()
    {
        if (topPlayerText == null) return;

        int maxKills = -1;
        PlayerRef currentLeader = PlayerRef.None;

        // Recorremos el diccionario de todos los jugadores buscando al mejor
        foreach (var kvp in GameState.Instance.Players)
        {
            if (kvp.Value.Kills > maxKills)
            {
                maxKills = kvp.Value.Kills;
                currentLeader = kvp.Key;
            }
        }

        // Si encontramos a alguien, imprimimos su nombre y sus kills
        if (currentLeader != PlayerRef.None)
        {
            string leaderName = GameState.Instance.GetPlayerName(currentLeader);
            topPlayerText.text = $"LÍDER: {leaderName} ({maxKills} KILLS)";
        }
        else
        {
            topPlayerText.text = "LÍDER: NINGUNO (0 KILLS)";
        }
    }
}