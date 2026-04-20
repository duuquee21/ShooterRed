using Fusion;
using TMPro;
using UnityEngine;

public class LocalPlayerHud : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI streakText;

    private void Awake()
    {
        TryResolveTextReferences();
    }

    private void Update()
{
    TryResolveTextReferences();

    if (healthText == null || scoreText == null || streakText == null)
        return;

    if (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null)
        return;

    if (!NetworkManager.Instance.Runner.IsRunning)
        return;

    // NUEVA LÍNEA: Esperar a que GameState esté Spawned
    if (!GameState.TryGetInstance(out GameState gameState) || gameState == null)
        return;

    PlayerRef me = NetworkManager.Instance.Runner.LocalPlayer;

    if (!gameState.TryGetPlayerData(me, out PlayerCombatData data))
        return;

    healthText.text = "HP: " + data.Health;
    scoreText.text = "Score: " + data.Score;
    streakText.text = "Racha: " + data.Streak;
}

    private void TryResolveTextReferences()
    {
        if (healthText != null && scoreText != null && streakText != null)
            return;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            string textName = texts[i].name.ToLowerInvariant();

            if (healthText == null && (textName.Contains("health") || textName.Contains("hp")))
            {
                healthText = texts[i];
                continue;
            }

            if (scoreText == null && textName.Contains("score"))
            {
                scoreText = texts[i];
                continue;
            }

            if (streakText == null && (textName.Contains("streak") || textName.Contains("racha")))
            {
                streakText = texts[i];
            }
        }

        if (texts.Length >= 3)
        {
            if (healthText == null)
                healthText = texts[0];

            if (scoreText == null)
                scoreText = texts[1];

            if (streakText == null)
                streakText = texts[2];
        }
    }
}