using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Se auto-instancia al arrancar el juego. Muestra métricas de red en la esquina
/// superior derecha solo cuando hay una sesión de Fusion activa.
/// No requiere ser añadido manualmente a ninguna escena.
/// </summary>
public class NetworkStatsHud : MonoBehaviour
{
    private TextMeshProUGUI _statsText;
    private GameObject _background;
    private NetworkRunner _runner;
    private float _timer;
    private const float UpdateInterval = 0.5f;

    // Se ejecuta automáticamente después de que carga cualquier escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        // Evitar duplicados si ya existe
        if (FindFirstObjectByType<NetworkStatsHud>() != null) return;

        GameObject go = new GameObject("NetworkStatsHud_Auto");
        go.AddComponent<NetworkStatsHud>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // Si ya hay otro (de una escena anterior con DontDestroyOnLoad), destruimos este
        NetworkStatsHud[] existing = FindObjectsByType<NetworkStatsHud>(FindObjectsSortMode.None);
        if (existing.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        CreateUI();
    }

    private void CreateUI()
    {
        // Canvas propio para que no interfiera con otros Canvas
        GameObject canvasGO = new GameObject("NetworkStatsCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Siempre encima de todo
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fondo semitransparente para que se lea bien
        _background = new GameObject("Background");
        _background.transform.SetParent(canvasGO.transform, false);
        Image bg = _background.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(1f, 1f);
        bgRect.anchorMax = new Vector2(1f, 1f);
        bgRect.pivot     = new Vector2(1f, 1f);
        bgRect.anchoredPosition = new Vector2(-10f, -10f);
        bgRect.sizeDelta = new Vector2(160f, 70f);

        // Texto de métricas
        GameObject textGO = new GameObject("StatsText");
        textGO.transform.SetParent(_background.transform, false);
        _statsText = textGO.AddComponent<TextMeshProUGUI>();
        _statsText.fontSize = 13f;
        _statsText.color = Color.white;
        _statsText.alignment = TextAlignmentOptions.TopLeft;
        _statsText.text = "Conectando...";
        RectTransform textRect = _statsText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 6f);
        textRect.offsetMax = new Vector2(-8f, -6f);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < UpdateInterval) return;
        _timer = 0f;

        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }

        bool inGame = _runner != null && _runner.IsRunning;
        _background.SetActive(inGame);

        if (!inGame) return;

        double rttMs = _runner.GetPlayerRtt(_runner.LocalPlayer) * 1000.0;
        int tick = _runner.Tick;
        float fps = 1f / Time.unscaledDeltaTime;

        string pingColor = rttMs < 80 ? "#00FF88" : rttMs < 150 ? "#FFCC00" : "#FF4444";

        _statsText.text = $"<color={pingColor}>Ping: {rttMs:F0} ms</color>\nTick:  {tick}\nFPS:  {fps:F0}";
    }
}
